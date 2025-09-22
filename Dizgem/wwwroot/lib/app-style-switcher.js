jQuery(function ($) {

    "use strict";

    function alanGuncelle(alan, deger) {
        $.ajax({
            url: '/Kullanici/TemaAyarlari',
            type: 'POST',
            data: JSON.stringify({ field: alan, val: deger }),
            contentType: 'application/json',
            success: function () { },
            error: function () { }
        });
    }

    $(document).on('change', 'input[name="color-theme-layout"]', function (e) {
        var $nesne = $('input[name="color-theme-layout"]:checked');

        alanGuncelle('renk_semasi', $nesne.attr('id'))
    });

    $(document).on('change', 'input[name="theme-layout"]', function (e) {
        var $nesne = $('input[name="theme-layout"]:checked');

        alanGuncelle('theme', $nesne.attr('id').toString().replaceAll('-layout', ''))
    });

    $(document).on('change', 'input[name="page-layout"]', function (e) {
        var $nesne = $('input[name="page-layout"]:checked');

        alanGuncelle('layout', $nesne.attr('id').toString().replaceAll('-layout', ''))
    });

    $(document).on('change', 'input[name="sidebar-type"]', function (e) {
        var $nesne = $('input[name="sidebar-type"]:checked');

        alanGuncelle('sidebar_type', $nesne.attr('id').toString().replaceAll('full-sidebar', 'full'))
    });

});