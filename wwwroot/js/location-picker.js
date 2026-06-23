/**
 * Chọn vị trí sân / hồ sơ: gợi ý địa chỉ + bản đồ ghim kéo thả (tọa độ chính xác hơn geocode tự động).
 */
window.SportHubLocationPicker = (function () {
    function init(options) {
        const addressInput = document.getElementById(options.addressInputId);
        const latInput = document.getElementById(options.latInputId);
        const lonInput = document.getElementById(options.lonInputId);
        const suggestionsEl = document.getElementById(options.suggestionsId);
        const mapContainer = document.getElementById(options.mapContainerId);
        const mapElId = options.mapId;
        const searchUrl = options.searchUrl || '/geo/search';

        if (!addressInput || !latInput || !lonInput || !suggestionsEl || !mapContainer) return;

        let map = null;
        let marker = null;
        let debounceTimer;

        function setCoords(lat, lon) {
            latInput.value = Number(lat).toFixed(6);
            lonInput.value = Number(lon).toFixed(6);
        }

        function showMap(lat, lon, label) {
            mapContainer.classList.remove('hidden');
            if (!map) {
                map = L.map(mapElId).setView([lat, lon], 17);
                L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                    attribution: '© OpenStreetMap'
                }).addTo(map);
                marker = L.marker([lat, lon], { draggable: true }).addTo(map);
                marker.on('dragend', function () {
                    const pos = marker.getLatLng();
                    setCoords(pos.lat, pos.lng);
                });
            } else {
                map.setView([lat, lon], 17);
                marker.setLatLng([lat, lon]);
            }
            if (label) marker.bindPopup(label).openPopup();
            setCoords(lat, lon);
            setTimeout(function () { map.invalidateSize(); }, 200);
        }

        function pickResult(item) {
            const label = item.display_name || item.displayName || addressInput.value;
            addressInput.value = label;
            suggestionsEl.classList.add('hidden');
            showMap(parseFloat(item.lat), parseFloat(item.lon), label);
        }

        function searchAddresses(query) {
            const q = (query || '').trim();
            if (q.length < 3) return Promise.resolve([]);

            // Ưu tiên endpoint nội bộ (đã tối ưu geocode), fallback sang OSM trực tiếp nếu lỗi.
            return fetch(searchUrl + '?q=' + encodeURIComponent(q) + '&limit=5', {
                headers: { Accept: 'application/json' }
            })
                .then(function (r) { return r.ok ? r.json() : null; })
                .then(function (data) {
                    if (Array.isArray(data) && data.length > 0) return data;

                    return fetch(
                        'https://nominatim.openstreetmap.org/search?format=json&addressdetails=1&countrycodes=vn&limit=5&q=' + encodeURIComponent(q),
                        { headers: { 'Accept-Language': 'vi' } }
                    )
                        .then(function (r) { return r.ok ? r.json() : []; })
                        .then(function (arr) {
                            if (!Array.isArray(arr)) return [];
                            return arr.map(function (x) {
                                return {
                                    lat: x.lat,
                                    lon: x.lon,
                                    display_name: x.display_name,
                                    source: 'nominatim'
                                };
                            });
                        });
                })
                .catch(function () {
                    return [];
                });
        }

        addressInput.addEventListener('input', function () {
            const query = this.value.trim();
            clearTimeout(debounceTimer);
            if (query.length < 3) {
                suggestionsEl.classList.add('hidden');
                return;
            }
            debounceTimer = setTimeout(function () {
                searchAddresses(query).then(function (data) {
                    suggestionsEl.innerHTML = '';
                    if (!data || data.length === 0) {
                        suggestionsEl.classList.add('hidden');
                        return;
                    }
                    data.forEach(function (item) {
                        const div = document.createElement('div');
                        div.className = 'p-3 hover:bg-slate-100 dark:hover:bg-slate-700 cursor-pointer border-b border-slate-100 dark:border-slate-800 last:border-0 text-sm';
                        div.style.color = '#1e293b';
                        const src = item.source ? ' <span style="font-size:10px;color:#94a3b8;">(' + item.source + ')</span>' : '';
                        div.innerHTML = '<div class="flex items-start gap-2"><span class="material-symbols-outlined text-primary text-base flex-shrink-0">location_on</span><span class="font-medium whitespace-normal break-words" style="color:#1e293b;">' + (item.display_name || '') + '</span>' + src + '</div>';
                        div.onclick = function () { pickResult(item); };
                        suggestionsEl.appendChild(div);
                    });
                    suggestionsEl.classList.remove('hidden');
                }).catch(console.error);
            }, 450);
        });

        addressInput.addEventListener('blur', function () {
            setTimeout(function () {
                if (!latInput.value && addressInput.value.trim().length >= 3) {
                    searchAddresses(addressInput.value.trim()).then(function (data) {
                        if (data && data.length > 0) pickResult(data[0]);
                    });
                }
            }, 250);
        });

        document.addEventListener('click', function (e) {
            if (!addressInput.contains(e.target) && !suggestionsEl.contains(e.target)) {
                suggestionsEl.classList.add('hidden');
            }
        });

        const initialLat = parseFloat(latInput.value);
        const initialLon = parseFloat(lonInput.value);
        if (!isNaN(initialLat) && !isNaN(initialLon)) {
            showMap(initialLat, initialLon, addressInput.value || 'Vị trí đã lưu');
        }
    }

    return { init: init };
})();
