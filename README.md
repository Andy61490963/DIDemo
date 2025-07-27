Table Schema 如下

Table FORM_FIELD_Master {
  SEQNO INT [not null, note: '排序用']
  ID UUID [primary key, note: '欄位設定的唯一識別編號（UUID）']
  FORM_NAME NVARCHAR(100) [not null, note: '表單識別名稱，例如 student_edit_form']
  BASE_TABLE_NAME NVARCHAR(100) [note: '實際要寫入 / 更新的資料表名稱，如 STUDENTS']
  VIEW_TABLE_NAME NVARCHAR(100) [note: '僅供展示的檢視表名稱，如 VW_STUDENT_FULL']
  BASE_TABLE_ID UUID [note: '實際要寫入 / 更新的資料表名稱，指回FORM_FIELD_Master的ID']
  VIEW_TABLE_ID UUID [note: '僅供展示的檢視表名稱，指回FORM_FIELD_Master的ID']
  PRIMARY_KEY NVARCHAR(100) [note: 'BASE_TABLE_NAME 的主鍵欄位名稱，如 STUDENT_ID']
}

Table FORM_FIELD_CONFIG {
  SEQNO INT [not null, note: '排序用']
  ID UUID [primary key, note: '欄位設定的唯一識別編號（UUID）']
  FORM_FIELD_Master_ID UUID [not null, note: '對應 FORM_FIELD_Master.ID']

  TABLE_NAME NVARCHAR(100) [not null, note: '實際對應的資料表名稱，如 STUDENTS']
  COLUMN_NAME NVARCHAR(100) [not null, note: '資料表中的欄位名稱，如 NAME']
  CONTROL_TYPE NVARCHAR(50) [not null, note: '欄位呈現類型：input / select / textarea / checkbox']
  DEFAULT_VALUE NVARCHAR(255) [note: '欄位預設值，可為靜態值或程式預設']
  IS_VISIBLE BIT [default: true, note: '是否顯示此欄位（true 顯示 / false 隱藏）']
  IS_EDITABLE BIT [default: true, note: '是否允許編輯（true 可編輯 / false 唯讀）']
  FIELD_ORDER INT [default: 0, note: '欄位在畫面中出現的排序順序']
  FIELD_GROUP NVARCHAR(100) [note: '欄位群組名稱，用於分群展示如「基本資料」']
  COLUMN_SPAN INT [default: 12, note: '欄位寬度，對應 Tailwind/Grid 系統的 col-span (1~12)']
  IS_SECTION_START BIT [default: false, note: '是否為新群組區塊開頭，用來產生卡片/分段']

  CREATE_USER NVARCHAR(50) [note: '建立人員帳號']
  CREATE_TIME DATETIME [note: '建立時間']
  EDIT_USER NVARCHAR(50) [note: '最後修改人員帳號']
  EDIT_TIME DATETIME [note: '最後修改時間']
}

Table FORM_FIELD_VALIDATION_RULE {
  SEQNO INT [not null, note: '排序用']
  ID UUID [primary key, note: '欄位驗證規則的唯一識別編號']
  FIELD_CONFIG_ID UUID [not null, note: '對應 FORM_FIELD_CONFIG.ID，指定驗證哪個欄位']
  VALIDATION_TYPE NVARCHAR(50) [not null, note: '驗證類型：required / max / min / regex / number / email']
  VALIDATION_VALUE NVARCHAR(255) [note: '驗證值，例如最大長度、正規表達式內容等']
  MESSAGE_ZH NVARCHAR(255) [note: '驗證錯誤時顯示的中文錯誤訊息']
  MESSAGE_EN NVARCHAR(255) [note: '驗證錯誤時顯示的英文錯誤訊息']
  VALIDATION_ORDER INT [default: 0, note: '執行驗證的優先順序，數字越小越早執行']
  CREATE_USER NVARCHAR(50) [note: '建立人員帳號']
  CREATE_TIME DATETIME [note: '建立時間']
  EDIT_USER NVARCHAR(50) [note: '最後修改人員帳號']
  EDIT_TIME DATETIME [note: '最後修改時間']
}

Table FORM_FIELD_DROPDOWN {
  SEQNO INT [not null, note: '排序用']
  ID UUID [primary key, note: '靜態下拉設定的唯一識別編號']
  FORM_FIELD_CONFIG_ID UUID [not null, note: '對應 FORM_FIELD_CONFIG.ID，指定是哪個欄位的下拉選單']
  ISUSESQL BIT [default: false, note: '是否使用 SQL 作為選項來源']
  DROPDOWNSQL NVARCHAR(255) [note: '若 ISUSESQL 為 true，此欄為 SQL 查詢語法（僅支援 SELECT）']
}

Table FORM_FIELD_DROPDOWN_OPTIONS {
  SEQNO INT [not null, note: '排序用']
  ID UUID [primary key, note: '靜態下拉選項的唯一識別編號']
  FORM_FIELD_DROPDOWN_ID UUID [not null, note: '對應 FORM_FIELD_DROPDOWN.ID，指定選項所屬下拉']

  OPTION_TEXT NVARCHAR(255) [not null, note: '選項顯示文字']
  SOURCE_TABLE NVARCHAR(100) [not null, note: '來源資料表名稱，例如 VW_EMPLOYEE_LIST']
  VALUE_FIELD NVARCHAR(100) [not null, note: 'SQL 結果中作為 option.value 的欄位名稱']
  TEXT_FIELD NVARCHAR(100) [not null, note: 'SQL 結果中作為 option.text 的欄位名稱']
  DESCRIPTION NVARCHAR(255) [note: '此資料來源的說明備註，可用於顯示']

  CREATE_USER NVARCHAR(50) [note: '建立人員帳號']
  CREATE_TIME DATETIME [note: '建立時間']
  EDIT_USER NVARCHAR(50) [note: '最後修改人員帳號']
  EDIT_TIME DATETIME [note: '最後修改時間']
}

Ref: FORM_FIELD_CONFIG.FORM_FIELD_Master_ID > FORM_FIELD_Master.ID
Ref: FORM_FIELD_VALIDATION_RULE.FIELD_CONFIG_ID > FORM_FIELD_CONFIG.ID
Ref: FORM_FIELD_DROPDOWN.FORM_FIELD_CONFIG_ID > FORM_FIELD_CONFIG.ID
Ref: FORM_FIELD_DROPDOWN_OPTIONS.FORM_FIELD_DROPDOWN_ID > FORM_FIELD_DROPDOWN.ID


說明文件如下

動態表單配置系統規格
本文件說明一個動態表單配置系統的功能架構與資料表定義。後台人員可透過設定，讓 AI 模型依據此規格產生對應的前台畫面及處理邏輯。本規格包含系統功能概念說明、前後台流程、資料表架構定義與欄位說明，以及設定範例。
功能架構概念
此系統允許開發人員在後台定義「表單」及其欄位配置，前台則依據配置動態生成表單畫面，並處理與資料庫之間的 CRUD（建立、讀取、更新、刪除）操作。核心概念如下：
•	表單配置（Form Configuration）： 後台人員定義一個表單的基本資訊，包括該表單對應的主資料表（用於資料寫入/更新）以及檢視用的資料來源（單一資料表或多表 JOIN 的檢視表）。
•	欄位配置（Field Configuration）： 針對每個表單，定義其包含的各個欄位如何呈現與運作。例如欄位名稱、顯示標籤、控制類型（文字框、下拉選單、文字區域等）、是否必填、預設值以及下拉選單的選項來源等。
•	前台動態生成： 前台根據後台設定的欄位配置動態渲染出輸入介面（表單）。使用者在前台填寫或修改資料時，系統會依照定義好的欄位規則進行驗證，並將資料寫回設定的主資料表中（新增或更新記錄）。
透過上述機制，開發人員可以不必針對每個表單手動編碼畫面，只需透過後台統一設定，即可讓 AI 模型或自動化程式生成對應的前台 UI 與處理邏輯。
後台功能與流程
後台提供表單與欄位配置的介面，讓系統管理員設定前台表單的結構與行為。後台主要流程如下：
1.	選擇主資料表： 管理員首先從資料庫中選擇一張作為表單輸入目標的主資料表（例如：A表）。該資料表就是前台表單進行 CRUD 操作時實際寫入或更新的對象。
2.	選擇檢視資料來源： 接著選擇一個檢視表（或資料庫 View，例如由 A表、B表、C表 JOIN 而成的檢視）作為前台表單顯示資料的來源。此檢視表可以結合多張表的資訊，用於在前台呈現豐富的內容（如下拉選單文字、關聯名稱等），而非僅限於主資料表的欄位。
3.	定義欄位設定： 在選定主表與檢視後，管理員針對該表單逐一建立欄位設定。每個欄位設定需包含以下資訊：
4.	欄位名稱： 對應資料庫中的實際欄位名稱（來自主資料表或相關聯的表）。
5.	顯示名稱： 前台畫面上顯示的欄位標籤名稱（例如「姓名」、「性別」等）。
6.	控制類型： 前台輸入控制的種類，例如文字輸入框 (input)、下拉選單 (select)、文字區域 (textarea)、核取方塊 (checkbox) 等。
7.	是否必填： 指定此欄位在前台是否為必填項目，以及相關的驗證規則。
8.	預設值： 此欄位的預設值（可為靜態值，或由程式/公式產生）。
9.	選項清單： 若控制類型為下拉選單或複選清單，需設定選項的取得方式。選項可以是靜態（直接列出固定選項）或動態（透過 SQL 查詢從資料庫擷取）。
10.	欄位群組與版面配置： 可選擇性地設定欄位所屬的群組（例如將欄位區分為「基本資料」、「其他資訊」區塊），以及在畫面網格系統中的寬度（如 12 欄欄位的橫跨比例）和是否開始新的區段等。這有助於前台版面美觀地排列欄位。
11.	定義驗證規則（可選）： 為每個欄位新增一系列驗證規則，如「必須填寫」、「最大/最小長度限制」、「格式（正則表達式）」、「數字/電子郵件格式」等，以及對應的錯誤訊息（中英雙語）。
12.	定義下拉選項來源（若適用）： 如果某欄位的控制類型為下拉選單且需要選項，可在配置中設定其選項來源：
13.	靜態選項： 直接在系統中列出選項清單（每個選項的顯示文字及對應的值）。
14.	動態選項： 提供一段 SQL SELECT 語句或指定一個資料來源（資料表/檢視）以及對應的值欄位與文字欄位，讓系統在呈現表單時自動執行查詢取得最新的選項清單（例如從代碼表或其他關聯表獲取下拉選單內容）。
設定完成後，這些表單與欄位的定義將存入資料庫中（詳見下方資料表結構）。AI 模型或前端程式可根據這些設定資料，自動生成相應的前台頁面與功能。
前台功能與流程
前台根據後台設定的配置資料動態構建表單介面，並引導使用者進行資料填寫與提交。前台主要流程如下：
1.	載入表單配置： 前端應用（或 AI 生成的程式碼）根據表單識別名稱（FORM_NAME）讀取後台設定的表單結構，包括該表單所需顯示的欄位清單、欄位屬性、驗證規則和選項清單來源等。
2.	渲染表單畫面： 前端根據配置動態生成表單的 HTML/元件結構：
3.	依照設定的欄位順序和欄位群組進行排列。例如，屬於「基本資料」群組的欄位會放在同一區塊中顯示，如果某欄位標記為一個新區段的開始，前端會產生相應的區隔或卡片版型。
4.	每個欄位使用指定的控制類型呈現：例如文字欄位使用輸入框，布林值可能使用核取方塊，下拉選單產生 <select> 選項等。若設定了預設值且目前為新增操作，則在欄位中顯示該預設值。
5.	對於下拉選單類型的欄位，前端根據設定取得選項：若為靜態選項，則直接從配置中載入定義的選項列表；若為動態選項，則執行預先設定的 SQL 查詢或讀取指定來源資料表，取得選項清單後再渲染。
6.	資料填入與即時驗證： 使用者在前台填寫各欄位時，系統可即時根據定義的驗證規則進行檢查。例如，若某欄位設定為必填且有最大長度限制，使用者未填或超過長度時，前端立即給出對應的錯誤提示（錯誤訊息內容依據後台設定的中/英文訊息）。
7.	提交與儲存（CRUD 操作）： 使用者完成表單填寫後提交：
8.	新增 (Create) 或 更新 (Update)： 前端將收集的資料依據表單設定進行處理，透過 API 或資料存取層傳送至後端。後端收到資料後，按照 FORM_FIELD_MASTER 中指定的主資料表，執行 Insert（新增）或 Update（更新）操作，將資料寫入/更新到正確的資料表欄位中（此過程需使用 PRIMARY_KEY 來判斷是新增還是更新既有記錄）。
9.	讀取 (Read)： 若是編輯既有資料的表單，在打開表單時，系統會根據 PRIMARY_KEY 從主資料表或定義的檢視 VIEW_TABLE_NAME 讀取現有資料，並填入表單欄位中供使用者修改。VIEW_TABLE_NAME 如果有多表 Join，可顯示相關的文字資訊（例如代碼轉文字）增強使用者體驗，但最終更新時仍針對 BASE_TABLE_NAME 實施。
10.	刪除 (Delete)： 系統可選擇提供刪除功能（例如在列表頁面或其他操作中），使用 PRIMARY_KEY 定位要刪除的記錄，然後對 BASE_TABLE_NAME 執行刪除操作。刪除通常不在表單頁面內進行，因此在本配置中主要關注新增與修改。
11.	結果反饋： 資料庫操作成功後，前端接收操作結果，給予使用者提示（例如「新增成功」、「更新完成」等）。若發生錯誤（例如驗證層遺漏或後端錯誤），前端則顯示錯誤訊息，以利使用者修正。
透過上述機制，前台表單的內容和行為可完全由後台資料驅動，使系統具有高度的動態調整能力：當需要修改表單欄位或規則時，只需在後台更新配置，前端（或 AI 產生的程式）即可在下次載入時自動反映最新的設定，無需額外程式碼開發。
資料表結構定義
以下是系統中涉及表單配置的主要資料表結構與說明，包括各資料表的用途以及欄位定義。這些資料表用於儲存後台所配置的表單、欄位、驗證規則與選項等資料。
FORM_FIELD_MASTER（表單主設定檔）
此表儲存每個動態表單的主檔資訊。一筆記錄代表一個可在前台操作的表單，定義了表單識別名稱、關聯的主資料表、檢視來源等。
•	SEQNO (INT, not null)：排序用序號，用於在列表中排序表單定義的顯示順序。
•	ID (UUID, primary key)：該表單設定的唯一識別碼（UUID）。
•	FORM_NAME (NVARCHAR(100), not null)：表單的識別名稱（程式代稱），例如 student_edit_form。前端載入特定表單配置時將依據此名稱查詢。
•	BASE_TABLE_NAME (NVARCHAR(100))：實際資料寫入/更新的主資料表名稱，例如 STUDENTS。前台提交資料時將對此表執行 Insert/Update 操作。
•	VIEW_TABLE_NAME (NVARCHAR(100))：前台顯示資料用的檢視名稱，例如 VW_STUDENT_FULL。通常為一個資料庫 View，可以將 BASE_TABLE_NAME 與其他相關表 Join 起來，以提供前端顯示所需的額外資訊（如代碼對應的文字說明）。如果為空，則表示直接使用 BASE_TABLE_NAME 進行資料存取與顯示。
•       BASE_TABLE_ID (UUID)：主表單對應的 FORM_FIELD_MASTER.ID，用於表單關聯。
•       VIEW_TABLE_ID (UUID)：檢視表單對應的 FORM_FIELD_MASTER.ID。
•	PRIMARY_KEY (NVARCHAR(100))：主鍵欄位名稱（在 BASE_TABLE_NAME 中的主鍵，例如 STUDENT_ID）。系統利用此欄位來辨識資料記錄，進行讀取現有資料或更新/刪除時使用。
FORM_FIELD_CONFIG（表單欄位設定檔）
此表存放特定表單下的欄位設定。一個表單（FORM_FIELD_MASTER 的一筆記錄）會對應多筆 FORM_FIELD_CONFIG 紀錄，每筆代表前台表單中的一個欄位配置，定義該欄位如何呈現與互動。
•	SEQNO (INT, not null)：排序用序號，用於控制此欄位在表單中出現的順序。
•	ID (UUID, primary key)：欄位設定的唯一識別碼（UUID）。
•	FORM_FIELD_MASTER_ID (UUID, not null)：所屬表單主檔的 ID，對應到 FORM_FIELD_MASTER.ID。表示此欄位設定隸屬哪一個表單。
•	TABLE_NAME (NVARCHAR(100), not null)：實際對應的資料表名稱，例如 STUDENTS。表示這個欄位的資料存放在哪個資料表中（通常是 BASE_TABLE_NAME 本身，但如果 VIEW 涉及多表，也可能是其他相關表的欄位）。
•	COLUMN_NAME (NVARCHAR(100), not null)：資料表中的欄位名稱，例如 NAME。對應資料庫實際的欄位，用於讀寫資料。
•	CONTROL_TYPE (NVARCHAR(50), not null)：前台欄位的控制呈現類型，例如：
•	"input" 表示文字輸入框 (單行輸入)
•	"select" 表示下拉選單
•	"textarea" 表示多行文字區域
•	"checkbox" 表示勾選方塊
等等。前端會根據這個類型生成相應的表單元件。
•	DEFAULT_VALUE (NVARCHAR(255))：欄位預設值。可設定一個靜態預設值（如固定字串、數字），或預留特殊關鍵字/函式由系統在執行時產生（例如当前日期、當前用戶等）。當建立新記錄時，前端表單將此預設值顯示於欄位中（除非另有指定）。
•	IS_VISIBLE (BIT, default true)：此欄位是否在前台表單中顯示。true 為顯示，false 為隱藏。隱藏的欄位前端不呈現，但仍可存在於配置中（可能用於邏輯判斷或默認傳值）。
•	IS_EDITABLE (BIT, default true)：此欄位在前台是否允許編輯。true 表示可編輯（正常輸入），false 表示唯讀（僅顯示不可修改）。常用於主鍵或某些不可變更的欄位。
•	FIELD_ORDER (INT, default 0)：欄位在畫面中的顯示順序序號。數字小者排在前面。通常和 SEQNO 用途相似，可視為另一種排序機制。
•	FIELD_GROUP (NVARCHAR(100))：欄位群組名稱，用於將表單中的欄位分組顯示。例如可將多個欄位歸類為「基本資料」、「聯絡資訊」等。前端可據此在表單中劃分區塊，加入分組標題等。
•	COLUMN_SPAN (INT, default 12)：欄位寬度，占前端版面網格欄位的比例。一般採用 12 格網格系統（如 Tailwind CSS 等框架），1~12 表示佔據的欄位格數。數字越大表示欄位呈現越寬。例：12 表示獨占一列（整排），6 表示佔該列一半寬度。
•	IS_SECTION_START (BIT, default false)：是否為新群組區塊的開頭。若為 true，表示從此欄位開始，前端應產生一個新的區段或卡片樣式，用於將表單區分為不同段落（配合 FIELD_GROUP 使用更佳）。例如表單前半部為「基本資料」，後半部為「其他資訊」，則「其他資訊」第一個欄位可標記此值為 true 以起始新段落。
（以下為系統管理欄位，記錄該設定的建立與修改資訊，AI 產生畫面時通常可忽略或僅供後台管理查詢） - CREATE_USER (NVARCHAR(50))：建立人員的帳號。 - CREATE_TIME (DATETIME)：建立時間。 - EDIT_USER (NVARCHAR(50))：最後修改人員的帳號。 - EDIT_TIME (DATETIME)：最後修改時間。
FORM_FIELD_VALIDATION_RULE（表單欄位驗證規則檔）
此表儲存各表單欄位對應的驗證規則設定。每筆記錄代表對某欄位的一個驗證要求（例如必填、長度限制、格式檢查等）。前台會依據這些規則對使用者輸入進行驗證，後台也可在接收資料時再次驗證。
•	SEQNO (INT, not null)：排序用序號，用於控制多個驗證規則的執行順序（數字小的規則優先檢查）。
•	ID (UUID, primary key)：驗證規則的唯一識別碼（UUID）。
•	FIELD_CONFIG_ID (UUID, not null)：關聯的欄位設定 ID，對應 FORM_FIELD_CONFIG.ID。表示此驗證規則屬於哪一個表單欄位。
•	VALIDATION_TYPE (NVARCHAR(50), not null)：驗證類型，常見類型例如：
•	required：必填（不可為空）
•	max：最大值或最大長度（需搭配 VALIDATION_VALUE）
•	min：最小值或最小長度
•	regex：正則表達式驗證
•	number：數字格式驗證（允許的數字範圍或純數字）
•	email：電子郵件格式驗證
•	...等。前端將根據不同的類型採取對應的驗證行為。
•	VALIDATION_VALUE (NVARCHAR(255))：驗證值或參數。依據不同 VALIDATION_TYPE，此欄存放相應的比較值或模式：
•	若類型為 max 或 min，此處可放置允許的最大/最小長度值（針對字串長度）或數字範圍值。
•	若類型為 regex，此處定義正則表達式的內容（不含開頭和結尾斜線/）。
•	其他類型如 required、email 等可能不需要數值，在此欄可留空或填預設。
•	MESSAGE_ZH (NVARCHAR(255))：驗證失敗時顯示的中文錯誤訊息。例如 VALIDATION_TYPE=required 時，可設定「此欄位為必填項目」作為提示。
•	MESSAGE_EN (NVARCHAR(255))：驗證失敗時顯示的英文錯誤訊息。如上例，可對應英文提示 "This field is required."。
•	VALIDATION_ORDER (INT, default 0)：驗證規則檢查順序，與 SEQNO 用途相近。數字較小者先執行，方便在前端分步驟提示使用者（例如先檢查必填，再檢查格式）。
•	CREATE_USER (NVARCHAR(50))：建立人員帳號。
•	CREATE_TIME (DATETIME)：建立時間。
•	EDIT_USER (NVARCHAR(50))：最後修改人員帳號。
•	EDIT_TIME (DATETIME)：最後修改時間。
FORM_FIELD_DROPDOWN（下拉選單設定檔）
此表專門存放欄位為「下拉選單類型」的選項取得方式設定。一個下拉選單欄位在 FORM_FIELD_CONFIG 中會對應至此表的一筆記錄，用於定義該欄位的選項清單從何而來（靜態列出或動態查詢）。
•	SEQNO (INT, not null)：排序用序號（一般用途不大，因每個欄位通常只對應一筆，下拉設定在系統中並非列表顯示，但為保持結構一致性而設置）。
•	ID (UUID, primary key)：下拉選單設定的唯一識別碼（UUID）。
•	FORM_FIELD_CONFIG_ID (UUID, not null)：關聯的欄位設定 ID，對應 FORM_FIELD_CONFIG.ID。表示這筆下拉設定是為哪一個表單欄位提供選項來源。
•	IS_USE_SQL (BIT, default false)：是否使用 SQL 查詢來作為選項來源：
•	若為 false，表示此欄位使用靜態方式提供選項，即選項清單預先定義在系統中（見下方 FORM_FIELD_DROPDOWN_OPTIONS 表）。
•	若為 true，表示使用動態 SQL 查詢方式取得選項。系統將使用下方 DROPDOWN_SQL 欄位中定義的查詢語法，在呈現表單時執行該查詢獲取選項。
•	DROPDOWN_SQL (NVARCHAR(255))：當 IS_USE_SQL=true 時，此欄位存放 SQL 查詢語句（限 SELECT 查詢）。查詢應回傳至少兩個欄位，一個作為選項的值 (option value)，一個作為選項的顯示文字 (option text)。例如：SELECT code AS value, name AS text FROM VW_CODE_GENDER。系統將執行此查詢並將結果用於產生下拉選單項目。
•	當 IS_USE_SQL=false 時，此欄位可為 NULL 或空，實際選項將從 FORM_FIELD_DROPDOWN_OPTIONS 表中取得。
FORM_FIELD_DROPDOWN_OPTIONS（下拉選單選項清單檔）
此表存放靜態下拉選單的選項內容。每個下拉欄位（FORM_FIELD_DROPDOWN 的記錄）對應多筆選項記錄，一筆代表一個選項。對於 IS_USE_SQL=false 的下拉欄位，系統會從此表讀取選項列表。若 IS_USE_SQL=true 則通常不使用本表（除非系統設計允許混合，但一般二擇一）。
•	SEQNO (INT, not null)：排序用序號，用於控制選項在下拉清單中的顯示順序（數字小者排在前）。
•	ID (UUID, primary key)：下拉選項的唯一識別碼（UUID）。
•	FORM_FIELD_DROPDOWN_ID (UUID, not null)：所屬下拉設定的 ID，對應 FORM_FIELD_DROPDOWN.ID。表示此選項屬於哪一個下拉欄位設定。
•	OPTION_TEXT (NVARCHAR(255), not null)：選項顯示文字，即在前端下拉清單中讓使用者看見的文字內容。例如「男」、「女」或「請選擇...」等。
•	SOURCE_TABLE (NVARCHAR(100), not null)：（進階用法） 選項來源的資料表或檢視名稱，例如 VW_EMPLOYEE_LIST。此欄位通常在動態選項時使用，指明查詢的來源資料表。若為靜態選項，此欄位亦可填入來源參考（例如指向一張代碼表名稱），或填入系統虛擬表識別。如非必要也可填入類似 "STATIC" 等值表示資料直接內建。
•	VALUE_FIELD (NVARCHAR(100), not null)：（進階用法） 在 SQL 結果或來源資料表中，作為選項值 (option.value) 的欄位名稱。前端提交表單時會將此值傳回後端。對於靜態選項，若 SOURCE_TABLE 非空，這裡可填該來源中代表值的欄位名稱；或在純靜態情況下，可儲存實際的值本身。
•	TEXT_FIELD (NVARCHAR(100), not null)：（進階用法） 在 SQL 結果或來源資料表中，作為選項顯示文字 (option.text) 的欄位名稱。對於靜態選項，如有來源表則填其文字欄位，或在純靜態情況下可與 OPTION_TEXT 相同。
•	DESCRIPTION (NVARCHAR(255))：此選項或資料來源的描述備註。可用於說明該選項集合的用途或其他備註資訊，供管理介面顯示或註解使用。
•	CREATE_USER (NVARCHAR(50))：建立人員帳號。
•	CREATE_TIME (DATETIME)：建立時間。
•	EDIT_USER (NVARCHAR(50))：最後修改人員帳號。
•	EDIT_TIME (DATETIME)：最後修改時間。
注意： FORM_FIELD_DROPDOWN_OPTIONS 的 SOURCE_TABLE、VALUE_FIELD、TEXT_FIELD 欄位通常與 FORM_FIELD_DROPDOWN 的 DROPDOWN_SQL 二擇一使用：
- 如果使用 DROPDOWN_SQL 動態查詢，開發者可以選擇不使用本表，因為 SQL 已涵蓋了值與文字的來源。
- 如果使用靜態選項，本表每筆記錄可視為一個選項，OPTION_TEXT 給出顯示文字，同時可在 VALUE_FIELD 欄位直接存放對應的值（例如代碼），TEXT_FIELD 欄位則可留空或重複 OPTION_TEXT，SOURCE_TABLE 可用來記錄該選項來源的分類（或留空）。
系統實作時，可簡化靜態選項的設計，只使用 OPTION_TEXT 和另外增加的 OPTION_VALUE 欄位來存放值。但在此定義中，VALUE_FIELD 及 TEXT_FIELD 提供了更彈性的結構來對應不同資料來源的欄位名稱。
資料表關聯關係
各資料表之間的關聯如下：
•	FORM_FIELD_CONFIG.FORM_FIELD_MASTER_ID 外鍵對應 FORM_FIELD_MASTER.ID：欄位設定表中的記錄必須隸屬某個表單主檔。
•	FORM_FIELD_VALIDATION_RULE.FIELD_CONFIG_ID 外鍵對應 FORM_FIELD_CONFIG.ID：驗證規則隸屬於特定的欄位設定。
•	FORM_FIELD_DROPDOWN.FORM_FIELD_CONFIG_ID 外鍵對應 FORM_FIELD_CONFIG.ID：下拉選單設定也隸屬於特定欄位設定（僅當欄位類型為 select 時有此對應）。
•	FORM_FIELD_DROPDOWN_OPTIONS.FORM_FIELD_DROPDOWN_ID 外鍵對應 FORM_FIELD_DROPDOWN.ID：下拉選單選項記錄隸屬於某個下拉欄位設定。
上述關聯確保了資料完整性：只有已存在的表單才能擁有欄位設定，只有已定義的欄位才能擁有驗證規則或下拉選項等設定。
配置使用範例
以下透過「學生資料編輯表單」的情境範例，說明各資料表之間如何配合使用，以及實際欄位設定內容。假設我們要建立一個學生基本資料的編輯表單，包括姓名、性別、導師和備註等欄位。
1. 定義表單主檔（FORM_FIELD_MASTER）：
假設建立一個表單，讓使用者編輯學生資訊。首先在 FORM_FIELD_MASTER 新增一筆記錄：
•	FORM_NAME： student_edit_form
•	BASE_TABLE_NAME： STUDENTS（主資料表，儲存學生基本資料）
•	VIEW_TABLE_NAME： VW_STUDENT_FULL（學生資訊檢視，例如 Students 表聯合其他表以取得完整資訊，如性別對應文字、導師姓名等。如果沒有複雜關聯，此處也可直接使用 STUDENTS 表或留空）
•	PRIMARY_KEY： STUDENT_ID（學生資料表的主鍵，例如學生編號）
•	SEQNO： 1（排序，若系統有多個表單，此表單顯示順序為第一）
•	其他欄位如 ID（UUID）會自動產生。
此設定表示：前台將有一個識別為 "student_edit_form" 的表單，主要對 STUDENTS 表進行資料寫入/更新，並可透過 VW_STUDENT_FULL 檢視來讀取和顯示學生相關資訊。
2. 定義欄位配置（FORM_FIELD_CONFIG）：
接著，為上述表單新增多筆欄位設定。假設我們需要以下欄位：
•	學生姓名（文字輸入框）
•	性別（下拉選單）
•	導師（下拉選單，由教師清單動態載入）
•	備註（多行文字區域）
在 FORM_FIELD_CONFIG 中新增記錄如下：
a. 學生姓名 欄位： - FORM_FIELD_MASTER_ID：對應上面建立的表單主檔記錄之 ID。
- TABLE_NAME：STUDENTS（此欄位屬於 Students 主表）。
- COLUMN_NAME：NAME（學生姓名欄位名稱）。
- CONTROL_TYPE：input（使用單行文字框讓使用者輸入姓名）。
- DEFAULT_VALUE：(空) （不預設值，或可留空字串）。
- IS_VISIBLE：true（前台顯示此欄位）。
- IS_EDITABLE：true（允許使用者編輯）。
- FIELD_ORDER：1（我們希望姓名是第一個欄位）。
- FIELD_GROUP：基本資料（將姓名歸類在「基本資料」區塊）。
- COLUMN_SPAN：6（佔一半寬度，讓它與另一個欄位並排在同一列）。
- IS_SECTION_START：true（標記為新區段開頭，開始「基本資料」區塊）。
b. 性別 欄位： - FORM_FIELD_MASTER_ID：同屬於 student_edit_form 表單。
- TABLE_NAME：STUDENTS（假設性別存儲在 Students 表中，例如存一個代碼）。
- COLUMN_NAME：GENDER 或 GENDER_CODE（性別代碼欄位名稱，視實際資料表欄位而定）。
- CONTROL_TYPE：select（下拉選單讓使用者選擇性別）。
- DEFAULT_VALUE：(空) （可選地預設為「請選擇」的提示值）。
- IS_VISIBLE：true。
- IS_EDITABLE：true。
- FIELD_ORDER：2（姓名之後第二個欄位）。
- FIELD_GROUP：基本資料（仍在基本資料區塊內）。
- COLUMN_SPAN：6（佔另一半寬度，與姓名欄位並排在同一列）。
- IS_SECTION_START：false（延續上一欄位的區塊，不新開區段）。
c. 導師 欄位（假設選取學生的導師或負責職員）： - FORM_FIELD_MASTER_ID：同上。
- TABLE_NAME：STUDENTS（假設 Students 表有一個 ADVISOR_ID 欄位存導師的員工編號）。
- COLUMN_NAME：ADVISOR_ID（導師之員工代號欄位）。
- CONTROL_TYPE：select（下拉選單，選擇導師）。
- DEFAULT_VALUE：(空) （不預設導師）。
- IS_VISIBLE：true。
- IS_EDITABLE：true。
- FIELD_ORDER：3（第三個欄位）。
- FIELD_GROUP：基本資料（也可算在基本資料區，或根據畫面需要決定群組）。
- COLUMN_SPAN：6（例如佔一半寬度，可與備註並排）。
- IS_SECTION_START：false（不新開區段）。
d. 備註 欄位： - FORM_FIELD_MASTER_ID：同上。
- TABLE_NAME：STUDENTS。
- COLUMN_NAME：REMARK（備註欄位）。
- CONTROL_TYPE：textarea（多行文字區域）。
- DEFAULT_VALUE：(空) 。
- IS_VISIBLE：true。
- IS_EDITABLE：true。
- FIELD_ORDER：4。
- FIELD_GROUP：其他資訊（將備註歸為另一組，例如「其他資訊」）。
- COLUMN_SPAN：12（佔一整列寬度，讓備註欄位獨占一行）。
- IS_SECTION_START：true（從此開始新區段「其他資訊」）。
上述四筆欄位設定就構成了學生編輯表單的欄位結構。姓名、性別、導師在第一區段「基本資料」橫向排列兩列兩欄（每列兩個欄位，各占一半寬度），備註在第二區段「其他資訊」獨佔一列。
3. 定義欄位驗證規則（FORM_FIELD_VALIDATION_RULE）：
為了確保使用者輸入資料的正確性，我們可以為上述欄位設定驗證規則，例如姓名和性別為必填，姓名不得超過一定長度等。在 FORM_FIELD_VALIDATION_RULE 中新增：
•	學生姓名 欄位（對應其 FIELD_CONFIG_ID）：
•	規則：required（必填）
o	VALIDATION_VALUE：(空)（必填不需要額外值）
o	MESSAGE_ZH：「請輸入學生姓名」
o	MESSAGE_EN：「Please enter the student name」
o	VALIDATION_ORDER：1
•	規則：max（最大長度）
o	VALIDATION_VALUE：50（假設姓名最多50個字）
o	MESSAGE_ZH：「姓名長度不可超過 50 個字」
o	MESSAGE_EN：「Name must not exceed 50 characters」
o	VALIDATION_ORDER：2
（以上表示：先檢查是否有輸入，再檢查長度上限）
•	性別 欄位：
•	規則：required（必選）
o	MESSAGE_ZH：「請選擇性別」
o	MESSAGE_EN：「Please select gender」
o	（性別選項通常有限定範圍，若需要也可增加如 regex 或 number 驗證確保值有效，但此處略過）
•	導師 欄位：
•	（若導師不是必選，可以不加必填規則；若需要，可加 required，同時應提供「請選擇導師」等提示）
•	備註 欄位：
•	（通常備註可選填，視需要可加入最大長度限制，如 max 200 字等）
透過這些驗證規則，前端在使用者提交或輸入時會即時提示，後端也能在接收資料時二次驗證，確保資料品質。
4. 定義下拉選單選項（FORM_FIELD_DROPDOWN 及 FORM_FIELD_DROPDOWN_OPTIONS）：
本範例中，性別和導師欄位都是下拉選單，因此需要設定其選項來源。
•	性別 欄位的選項：因性別選項固定且數量少（例如「男」、「女」），我們可採用靜態方式：
•	在 FORM_FIELD_DROPDOWN 新增一筆記錄：
o	FORM_FIELD_CONFIG_ID：對應前述性別欄位的 ID。
o	IS_USE_SQL：false（不使用SQL查動態查詢，因為性別是固定選項）。
o	DROPDOWN_SQL：(空)。
•	接著在 FORM_FIELD_DROPDOWN_OPTIONS 表中新增兩筆選項記錄，關聯到上述 DROPDOWN 設定的 ID：
a.	「男」 選項：
b.	OPTION_TEXT：男（前端顯示「男」）
c.	VALUE_FIELD：M（其對應的值，例如資料庫可能以 'M' 表示男性。由於本表沒有獨立的 Option Value 欄位，此處可以使用 VALUE_FIELD 欄位存放實際值）
d.	TEXT_FIELD：男（或者留空。此欄在靜態情況下可與 OPTION_TEXT 相同）
e.	SOURCE_TABLE：(空或"STATIC")（靜態選項可不特別指定來源資料表）
f.	DESCRIPTION：「性別選項」（可選填描述此選項所屬分類或用途）
g.	「女」 選項：
h.	OPTION_TEXT：女
i.	VALUE_FIELD：F（資料庫以 'F' 表示女性）
j.	TEXT_FIELD：女
k.	SOURCE_TABLE：(空或"STATIC")
l.	DESCRIPTION：「性別選項」
•	這樣前端渲染該下拉選單時，就會出現「男」和「女」兩個選項，其對應的提交值分別是 'M' 和 'F'。
•	導師 欄位的選項：假設導師選單需要從現有的員工清單中動態取得（例如選擇教師），我們採用動態 SQL 方式：
•	在 FORM_FIELD_DROPDOWN 新增一筆記錄：
o	FORM_FIELD_CONFIG_ID：對應導師欄位的 ID。
o	IS_USE_SQL：true（使用 SQL 動態取得）。
o	DROPDOWN_SQL：例如：SELECT EMP_ID AS value, EMP_NAME AS text FROM VW_EMPLOYEE_LIST WHERE ROLE = 'TEACHER'。
（此 SQL 從檢視 VW_EMPLOYEE_LIST 查詢所有角色為教師的員工，返回員工編號作為選項值，員工姓名作為選項顯示文字。可根據實際資料表結構調整查詢條件。）
•	使用 SQL 方式時，通常不需要在 FORM_FIELD_DROPDOWN_OPTIONS 定義每筆選項（因為選項將由查詢結果動態產生）。但若需要附加靜態選項（例如「無指定」選項），也可搭配 OPTIONS 表使用。
完成以上設定後，前端在渲染性別欄位時，會直接使用我們定義的兩筆靜態選項；而渲染導師欄位時，會執行指定的 SQL 從資料庫取得導師清單。例如，假設資料庫中有兩位教師，ID 分別為 101 和 102，姓名為 Alice 和 Bob，則查詢結果將產生選項「Alice」和值 101、「Bob」和值 102，前端下拉清單中就會出現這兩個名字供選擇。
5. 前台畫面效果與資料流：
根據上述配置，當 AI 模型或前端系統生成「學生資料編輯表單」時：
•	表單標題可以顯示為「學生資料編輯」（可由 FORM_NAME 或其他映射取得更友善的名稱）。
•	表單內容分為兩個區塊：「基本資料」和「其他資訊」：
•	基本資料區塊：第一列包含「學生姓名」(文字框) 和 「性別」(下拉)，第二列包含「導師」(下拉) 及第二列另一半可能留白（因我們將導師和備註各佔一半一整列，但也可以將導師與備註並排，依配置靈活調整）。
•	其他資訊區塊：包含「備註」(多行文字框)，獨占一整行。
•	若為編輯既有學生資料，系統會透過 VW_STUDENT_FULL 載入該學生當前資料。例如學生姓名、性別代碼、以及其對應導師的 ID 等。前端會將姓名填入姓名欄位，性別下拉根據代碼選中「男/女」，導師下拉則選擇對應教師的姓名，備註欄位填入既有備註。
•	使用者可修改這些欄位的值。在離開欄位或提交時，系統按我們設定的驗證規則檢查：
•	若姓名留白，則提示「請輸入學生姓名」且無法提交。
•	若姓名超過50字，提示長度限制錯誤。
•	若性別未選擇，則提示需選擇。
•	導師欄若未選且非必填可允許為空；如我們未設必填則可空白。
•	當使用者按下「儲存」時，前端將收集修改後的資料（例如姓名="王小明", 性別代碼="M", 導師ID=101, 備註="轉班生" 等）組成請求，經由 API 傳給後端。後端依據 PRIMARY_KEY（如 STUDENT_ID=12345）判斷是更新已有學生，遂執行：
 	UPDATE STUDENTS 
   SET NAME='王小明', GENDER='M', ADVISOR_ID=101, REMARK='轉班生' 
 WHERE STUDENT_ID=12345;
 	更新成功後返回結果給前端。
•	前端接收到成功回應後，可能導航使用者回列表頁並提示「學生資料更新成功」。若後端回傳錯誤（例如違反唯一約束等），則前端顯示「更新失敗」及相關錯誤訊息。
6. 其他備註：
•	若日後需增加或調整表單欄位（例如新增「出生日期」欄位），只需在後台相應表單的配置下增修記錄（FORM_FIELD_CONFIG 等），前端載入時即可自動更新表單，不需修改前端程式碼。
•	AI 模型在讀取這些配置時，可以依照 Markdown 文件提供的結構，自動生成資料存取層和表單 UI。開發人員也可將此文件作為參考實現程式碼。由於包含詳細的欄位屬性與驗證規則，AI 可以據此產生對應的前端表單（HTML/JSX/組件定義）以及後端處理流程（例如資料庫操作、驗證邏輯），大幅減少人工編碼。
•	在實際實現中，需注意資料安全與驗證的冗餘：即使前端有驗證，後端在接收資料時也應根據驗證規則再次檢查，確保惡意繞過前端驗證的請求不會損壞資料完整性。
以上範例與說明展示了動態表單配置系統的整體設計。透過這套配置，AI 模型或自動化工具可以理解表單的結構與規則，進而正確地生成 UI 畫面和資料處理邏輯，實現快速開發與靈活維護。
