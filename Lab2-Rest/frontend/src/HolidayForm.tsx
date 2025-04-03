import { useState } from "react";
import "./HolidayForm.css";

const HolidayForm: React.FC = () => {
    const [name, setName] = useState("Petr");
    const [country, setCountry] = useState("US");
    const [month, setMonth] = useState(10);
    const [day, setDay] = useState(25);
    const [results, setResults] = useState<any>(null);
    const [buttonType, setButtonType] = useState('');

    const fetchHolidaysByNameJson= async (event: React.FormEvent) => {
        event.preventDefault();

        try {
            const response = await fetch(
                `http://localhost:5100/api/holidays/fetch/name?name=${name}&json=true`
            );

            if (!response.ok) {
                throw new Error("Network error!");
            }

            const data = await response.json();
            setResults(data);
        } catch (error) {
            console.error(error);
            setResults({ error: "Error fetching data!" });
        }
    };
    const fetchHolidaysByDateJson = async (event: React.FormEvent) => {
        event.preventDefault();

        try {
            const response = await fetch(
                `http://localhost:5100/api/holidays/fetch/date?month=${month}&day=${day}&json=true`
            );

            if (!response.ok) {
                throw new Error("Network error!");
            }

            const data = await response.json();
            setResults(data);
        } catch (error) {
            console.error(error);
            setResults({ error: "Error fetching data!" });
        }
    };
    const fetchHolidaysByNameHtml = async (event: React.FormEvent) => {
        event.preventDefault();
        try {
            const response = await fetch(
                `http://localhost:5100/api/holidays/fetch/name?name=${name}&json=false`
            );

            if (!response.ok) {
                throw new Error("Network error!");
            }

            const html = await response.text();
            const newWindow = window.open();
            if (newWindow) {
                newWindow.document.write(html);
            }
        } catch (error) {
            console.error(error);
        }
    };

    const fetchHolidaysByDateHtml = async (event: React.FormEvent) => {
        event.preventDefault();
        try {
            const response = await fetch(
                `http://localhost:5100/api/holidays/fetch/date?month=${month}&day=${day}&json=false`
            );

            if (!response.ok) {
                throw new Error("Network error!");
            }

            const html = await response.text();
            const newWindow = window.open();
            if (newWindow) {
                newWindow.document.write(html);
            }
        } catch (error) {
            console.error(error);
        }
    };



    const handleButtonClick = (type: string) => {
        setButtonType(type);
    };

    const handleSubmit = async (event: React.FormEvent) => {
        event.preventDefault();
        if (buttonType === 'nameHtml') {
            fetchHolidaysByNameHtml(event);
        } else if (buttonType === 'dateHtml') {
            fetchHolidaysByDateHtml(event);
        } else if(buttonType === 'nameJson') {
            fetchHolidaysByNameJson(event);
        } else if(buttonType === 'dateJson') {
            fetchHolidaysByDateJson(event);
        }
    };

    return (
        <div className="container">
            <h1 className="title">Lab2 REST API</h1>
            <form onSubmit={handleSubmit} className="form">
                <div>
                    <label className="label">Imię:</label>
                    <input
                        type="text"
                        value={name}
                        onChange={(e) => setName(e.target.value)}
                        required
                        className="input"
                    />
                </div>

                <div>
                    <label className="label">Kraj:</label>
                    <input
                        type="text"
                        value={country}
                        onChange={(e) => setCountry(e.target.value)}
                        required
                        className="input"
                    />
                </div>

                <div className="grid-container">
                    <div>
                        <label className="label">Dzień:</label>
                        <input
                            type="number"
                            value={day}
                            onChange={(e) => setDay(Number(e.target.value))}
                            required
                            className="input"
                        />
                    </div>
                    <div>
                        <label className="label">Miesiąc:</label>
                        <input
                            type="number"
                            value={month}
                            onChange={(e) => setMonth(Number(e.target.value))}
                            required
                            className="input"
                        />
                    </div>
                </div>
                <h2>Pobierz Imieniny i święta</h2>
                <div className="buttons">
                    <div>
                        <button
                            type="submit"
                            className="button"
                            onClick={() => handleButtonClick('nameHtml')}
                        >
                            Przez Imie - HTML
                        </button>
                        <button
                            type="submit"
                            className="button"
                            onClick={() => handleButtonClick('dateHtml')}
                        >
                            Przez Date - HTML
                        </button>
                    </div>
                    <div>
                        <button
                            type="submit"
                            className="button"
                            onClick={() => handleButtonClick('nameJson')}
                        >
                            Przez Imie - JSON
                        </button>
                        <button
                            type="submit"
                            className="button"
                            onClick={() => handleButtonClick('dateJson')}
                        >
                            Przez Date - JSON
                        </button>
                    </div>
                </div>
            </form>

            {results && (
                <div className="results">
                    <h2 className="results-title">Wyniki:</h2>
                    {results.error ? (
                        <p className="error">{results.error}</p>
                    ) : (
                        <>
                            {results.svatky && results.svatky.length > 0 ? (
                                <div className="section">
                                    <h3 className="section-title">Imieniny:</h3>
                                    <ul className="list">
                                    {results.svatky.map((s: any, index: number) => {
                                            const day = s.date.slice(0, 2).padStart(2, '0');
                                            const month = s.date.slice(2, 4).padStart(2, '0');
                                        const relatedHolidaysTmp = Array.isArray(results.abstractApi[0])
                                            ? results.abstractApi.filter((h: any) =>
                                                h.some((holiday: any) => {
                                                    const holidayDay = holiday.day.toString().padStart(2, '0');
                                                    const holidayMonth = holiday.month.toString().padStart(2, '0');
                                                    return holidayDay === day && holidayMonth === month;
                                                })
                                            )
                                            : results.abstractApi.filter((holiday: any) => {
                                                const holidayDay = holiday.day.toString().padStart(2, '0');
                                                const holidayMonth = holiday.month.toString().padStart(2, '0');
                                                return holidayDay === day && holidayMonth === month;
                                            }
                                            );
                                        const relatedHolidays = Array.isArray(relatedHolidaysTmp[0])
                                            ? relatedHolidaysTmp :
                                            [relatedHolidaysTmp];
                                            return (
                                                <li key={index}>
                                                    {s.name} - {s.date.slice(0, 2)}.{s.date.slice(2, 4)}
                                                    {relatedHolidays.length > 0 && (
                                                        <ul>
                                                            {relatedHolidays.map((h: any, idx: number) => (
                                                                h.map((holiday: any, hIdx: number) => (
                                                                    <li key={`${idx}-${hIdx}`}>{holiday.name}</li>

                                                                ))
                                                            ))}

                                                        </ul>

                                                    )}
                                                </li>
                                            );
                                        })}
                                    </ul>
                                </div>
                            ) : (
                                <p className="error">Wybierz se czeskie imie kreciku</p>
                            )}
                        </>
                    )}
                </div>
            )}
        </div>
    );
};

export default HolidayForm;
