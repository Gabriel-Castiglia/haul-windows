#
#  haul.py
#  Haul: yt-dlp extractor plugin for links the pinned yt-dlp version does not handle.
#
#  Copyright (C) 2026 Gabriel Castiglia. Licensed under the GNU GPL, version 2 only.
#  Date: October 24, 2026
#  SPDX-License-Identifier: GPL-2.0-only
#
#  This program is free software: you can redistribute it and/or modify it under the terms of
#  the GNU General Public License, version 2, as published by the Free Software Foundation.
#  It is distributed WITHOUT ANY WARRANTY. See the LICENSE file for details.
#
#  Loaded with --plugin-dirs. Plugin extractors are tried before the built-in ones, so each
#  class below only matches the links it fixes; everything else reaches yt-dlp untouched.
#

import json
import re
import time
import urllib.parse

from yt_dlp.extractor.common import InfoExtractor
from yt_dlp.extractor.vimeo import VimeoIE
from yt_dlp.extractor.youtube import YoutubeIE
from yt_dlp.networking import HEADRequest
from yt_dlp.networking.exceptions import HTTPError
from yt_dlp.utils import ExtractorError, determine_ext, unescapeHTML


def _vimeo_player(ie, url):
    # The Vimeo player answers some requests with 401 at random (about one in ten when measured
    # on 24-Oct-2026), and a repeat a moment later succeeds. Up to four attempts, three seconds apart.
    vimeo = ie._downloader.get_info_extractor(VimeoIE.ie_key())
    for attempt in range(4):
        try:
            return vimeo.extract(url)
        except ExtractorError as error:
            if attempt == 3 or not isinstance(error.cause, HTTPError) or error.cause.status != 401:
                raise
            ie.report_warning(f'Vimeo answered 401, retrying ({attempt + 1}/3)')
            time.sleep(3)


class HaulVimeoIE(InfoExtractor):
    # vimeo.com/<id> pages now require a logged-in account in yt-dlp, while the embedded player
    # serves the same public video without one. The page link is rewritten to the player link,
    # keeping the private hash of unlisted videos (vimeo.com/<id>/<hash>).
    IE_NAME = 'haul:vimeo'
    _VALID_URL = r'''(?x)
        https?://(?:www\.)?vimeo\.com/
        (?:channels/[^/?#]+/|groups/[^/?#]+/videos/)?
        (?P<id>\d+)(?:/(?P<hash>[\da-f]{6,}))?/?(?:[?#]|$)'''

    def _real_extract(self, url):
        video_id, unlisted_hash = self._match_valid_url(url).group('id', 'hash')
        if not unlisted_hash:
            unlisted_hash = urllib.parse.parse_qs(urllib.parse.urlparse(url).query).get('h', [None])[0]
        player = f'https://player.vimeo.com/video/{video_id}'
        if unlisted_hash:
            player += f'?h={unlisted_hash}'
        return _vimeo_player(self, player)


def _substack_preloads(ie, webpage, display_id):
    # Substack moved its page data from window._preloads = JSON.parse("…") to a JSON script tag;
    # both forms are accepted.
    data = ie._search_regex(
        r'<script[^>]+id=["\']preloads-json["\'][^>]*>(?P<json>.+?)</script>',
        webpage, 'preloads', default=None, group='json')
    if data:
        return json.loads(data)
    encoded = ie._search_regex(
        r'window\._preloads\s*=\s*JSON\.parse\((?P<json>"(?:[^"\\]|\\.)*")\)',
        webpage, 'preloads', group='json')
    return json.loads(json.loads(encoded))


def _substack_embeds(url, body):
    # Players embedded in a post's text (Vimeo and YouTube). Vimeo embeds carry the post as
    # referer, which embed-restricted videos require.
    found = []
    for embed in VimeoIE._extract_embed_urls(url, body):
        found.append(VimeoIE._smuggle_referrer(unescapeHTML(embed), url))
    for embed in YoutubeIE._extract_embed_urls(url, body):
        found.append(unescapeHTML(embed))
    return list(dict.fromkeys(found))


class HaulSubstackIE(InfoExtractor):
    # A Substack post: an uploaded video or podcast, or players embedded in the text.
    IE_NAME = 'haul:substack'
    _VALID_URL = r'https?://(?P<host>[\w-]+\.substack\.com)/p/(?P<id>[\w-]+)'

    def _real_extract(self, url):
        host, display_id = self._match_valid_url(url).group('host', 'id')
        webpage = self._download_webpage(url, display_id)
        post = _substack_preloads(self, webpage, display_id)['post']
        title = post.get('title')
        base = f'https://{host}'

        if post.get('type') == 'podcast' and post.get('podcast_url'):
            fmt = {'url': post['podcast_url']}
            if not determine_ext(fmt['url'], default_ext=None):
                fmt['ext'] = determine_ext(self._request_webpage(
                    HEADRequest(fmt['url']), display_id, 'Resolving podcast file extension').url)
            return {'id': str(post.get('id') or display_id), 'title': title, 'formats': [fmt]}

        upload = (post.get('videoUpload') or {}).get('id') or post.get('video_upload_id')
        if upload:
            formats, subtitles = self._extract_m3u8_formats_and_subtitles(
                f'{base}/api/v1/video/upload/{upload}/src?type=hls', display_id, 'mp4', fatal=False)
            formats.append({'url': f'{base}/api/v1/video/upload/{upload}/src?type=mp4'})
            return {'id': str(post.get('id') or display_id), 'title': title,
                    'formats': formats, 'subtitles': subtitles}

        embeds = _substack_embeds(url, post.get('body_html') or webpage)
        if not embeds:
            raise ExtractorError('This post has no video or audio', expected=True)
        if len(embeds) == 1:
            if VimeoIE.suitable(embeds[0]):
                return _vimeo_player(self, embeds[0])
            return self.url_result(embeds[0])
        return self.playlist_from_matches(embeds, display_id, title)


class HaulSubstackTagIE(InfoExtractor):
    # A Substack tag page (/t/<tag>): the tagged posts that carry video or audio, as a
    # playlist ordered from the oldest to the newest (the API's newest-first order, reversed),
    # so a series keeps its episode order.
    IE_NAME = 'haul:substack:tag'
    _VALID_URL = r'https?://(?P<host>[\w-]+\.substack\.com)/t/(?P<id>[\w-]+)'
    _PAGE = 50

    def _real_extract(self, url):
        host, slug = self._match_valid_url(url).group('host', 'id')
        webpage = self._download_webpage(url, slug)
        tag = _substack_preloads(self, webpage, slug)['tagPagePreloads']['postTag']

        posts = []
        for offset in range(0, 10000, self._PAGE):
            page = self._download_json(
                f'https://{host}/api/v1/archive', slug, f'Downloading posts {offset + 1}+',
                query={'sort': 'new', 'post_tag_id': tag['id'], 'offset': offset, 'limit': self._PAGE})
            posts.extend(page)
            if len(page) < self._PAGE:
                break

        def has_media(post):
            body = post.get('body_html')
            return (post.get('type') in ('video', 'podcast') or post.get('video_upload_id')
                    or body is None or re.search(r'player\.vimeo\.com|youtube(?:-nocookie)?\.com/embed', body))

        entries = [
            self.url_result(f'https://{host}/p/{post["slug"]}', HaulSubstackIE, str(post.get('id')), post.get('title'))
            for post in reversed(posts)
            if post.get('slug') and has_media(post)]
        return self.playlist_result(entries, tag['id'], tag.get('name') or slug)
