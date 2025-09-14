document.addEventListener('DOMContentLoaded', function () {
    // Функционал смены фона
    const changeBackgroundBtn = document.getElementById('changeBackgroundBtn');
    const backgroundModal = document.getElementById('backgroundModal');
    const modalClose = document.querySelector('.modal-close');
    const backgroundOptions = document.querySelectorAll('.background-option');

    // Открытие модального окна
    changeBackgroundBtn.addEventListener('click', function (e) {
        e.preventDefault();
        backgroundModal.style.display = 'flex';
    });

    // Закрытие модального окна
    modalClose.addEventListener('click', function () {
        backgroundModal.style.display = 'none';
    });

    // Выбор фона
    backgroundOptions.forEach(option => {
        option.addEventListener('click', function () {
            const imageUrl = this.dataset.image;

            // Сохраняем в localStorage
            localStorage.setItem('backgroundImage', imageUrl);

            // Применяем фон
            document.body.style.backgroundImage = `url('${imageUrl}')`;

            // Закрываем модальное окно
            backgroundModal.style.display = 'none';
        });
    });

    // Загрузка сохраненного фона при загрузке страницы
    document.addEventListener('DOMContentLoaded', function () {
        const savedBackground = localStorage.getItem('backgroundImage');
        if (savedBackground) {
            document.body.style.backgroundImage = `url('${savedBackground}')`;
        }
    });
});