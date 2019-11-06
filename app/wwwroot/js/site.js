// setup Bootstrap 4 Forms Validation
(function() {
    "use strict";
    window.addEventListener("load",
        function() {
            // Fetch all the forms we want to apply custom Bootstrap validation styles to
            var forms = document.getElementsByClassName("needs-validation");
            // Loop over them and prevent submission
            var validation = Array.prototype.filter.call(forms,
                function(form) {
                    form.addEventListener("submit",
                        function(event) {
                            if (form.checkValidity() === false) {
                                event.preventDefault();
                                event.stopPropagation();
                                // cancel the spinner
                                window.setTimeout(function () {
                                    $('div#progress-overlay').hide();
                                }, 100);
                            }
                            form.classList.add("was-validated");
                        },
                        false);
                });
        },
        false);
})();

$(document).ready(function () {

    // Init Tooltips
    $('[data-toggle="tooltip"]').tooltip();

    //caches a jQuery object containing the header element
    var header = $("#scj-header-nav");
    $(window).scroll(function() {
        var scroll = $(window).scrollTop();
        if (scroll >= 85) {
            header.addClass("affixed");
        } else {
            header.removeClass("affixed");
        }
    });

    // phone number inputs - form field data tidy up
    $("input.phone-input").change(function () {
        var phone = this.value.replace(/[^0-9]/g, "");

        if (phone.indexOf("1") === 0) {
            phone = phone.substr(1);
        }

        if (phone.length === 10) {
            this.value = phone.replace(/(\d{3})(\d{3})(\d{4})/, "$1-$2-$3");
        }
    });

    // Set up the progress spinner overlay
    $('body').on('click', 'input.progress-spinner, a.progress-spinner, button.progress-spinner', function () {
        $('div#progress-overlay').show();
        $('div#progress-overlay-spinner').spin();
    });

    // kills the spinner when the back button is pressed 
    window.onunload = function () { };

    // CoA additional questions for Civil cases
    //$("#btnNextCoa").click(function (e) {
    //    e.preventDefault();
    //    $("#pnlCoaCivil").toggle();
    //    $("#btnNextCoa").toggle();
    //    $("#btnSubmitCoa").toggle();

    //    // scroll down to additional questions panel
    //    if ($("#btnSubmitCoa").css("display") !== "none") {
    //        var panel = $("div[id='pnlCoaCivil']");
    //        $("html,body").animate({ scrollTop: panel.offset().top }, "fast");
    //    }
    //});

    // Give active style class to checked radio button input
    var $radiobtns = $('input[type="radio"]');
    $radiobtns.click(function () {
        $radiobtns.each(function () {
            $(this).parent().toggleClass('active', this.checked);
        });
    });


    //Display last row of questions for Civil case if previous questions are answered Yes
    var toggleCivil = function () {
        var $Civil_CertificateOfReadiness = $('#Civil_CertificateOfReadiness input[type="radio"]:checked').val();
        var $Civil_DateIsAgreed = $('#Civil_DateIsAgreed input[type="radio"]:checked').val();
        if ($Civil_CertificateOfReadiness === 'true' && $Civil_DateIsAgreed === 'true') {
            $('#CivilAdditionalQs').toggle();
        }
    };

    $('#Civil_CertificateOfReadiness input[type="radio"]').click(toggleCivil);
    $('#Civil_DateIsAgreed input[type="radio"]').click(toggleCivil);


    //Display last row of questions for Criminal case if previous questions are answered Yes
    var toggleCriminal = function () {
        var $Criminal_LowerCourtOrder = $('#Criminal_LowerCourtOrder input[type="radio"]:checked').val();
        var $Criminal_DateIsAgreed = $('#Criminal_DateIsAgreed input[type="radio"]:checked').val();
        if ($Criminal_LowerCourtOrder === 'true' && $Criminal_DateIsAgreed === 'true') {
            $('#CriminalAdditionalQs').toggle();
        }
    }; 

    $('#Criminal_LowerCourtOrder input[type="radio"]').click(toggleCriminal);
    $('#Criminal_DateIsAgreed input[type="radio"]').click(toggleCriminal);
});

// Called by Vue when a timeslot is selected
function validateCaseDate(containerId, bookingDate) {
    //set hidden fields based on the swiper selected dates
    $("#hidContainerId").val(containerId);
    $("#hidDate").val(bookingDate);

    //submit the form to go to the next page
    $("#btnSearch").trigger("click");
}

