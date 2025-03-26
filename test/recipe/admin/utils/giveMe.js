import { createError } from "#app";

// value or default function, named $ for quick access
function $(value, defaultValue) {
  return value === undefined ? defaultValue : value;
}

export default {
  aCardLink({ route, icon, title, description, disabled, disabledReason } = {}) {
    route = $(route, "/test-route");
    icon = $(icon, "pi pi-heart");
    title = $(title, "Test");
    description = $(description, "Test description is given for testing purposes");
    disabled = $(disabled, false);
    disabledReason = $(disabledReason, disabled ? "REASON" : "");

    return {
      type: "CardLink",
      schema: { route, icon, title, description, disabled, disabledReason }
    };
  },

  aDataPanel({ title, collapsed, content } = {}) {
    title = $(title, "Test Title");
    collapsed = $(collapsed, false);
    content = $(content, this.anExpected());

    return {
      type: "DataPanel",
      schema: { title, collapsed, content }
    };
  },

  aDataTable({ columns, rowCountWhenLoading, data } = {}) {
    columns = $(columns, [
      this.aDataTableColumn({ prop: "test" })
    ]);
    rowCountWhenLoading = $(rowCountWhenLoading, null);
    data = $(data, [
      { test: "value 1" },
      { test: "value 2" },
      { test: "value 3" }
    ]);

    return {
      type: "DataTable",
      schema: { columns, rowCountWhenLoading },
      data: { type: "Inline", value: data }
    };
  },

  aDataTableColumn({ title, prop, minWidth, component } = {}) {
    title = $(title, "Test");
    prop = $(prop, "test");
    minWidth = $(minWidth, false);
    component = $(component, this.anExpected());

    return {
      title,
      prop,
      minWidth,
      component
    };
  },

  anErrorPage({ errorInfos, footerInfo, safeLinks, safeLinksMessage, data } = {}){
    errorInfos = $(errorInfos, [this.anErrorPageInfo()]);
    footerInfo = $(footerInfo, "Test footer info");
    safeLinks = $(safeLinks, [this.anExpected()]);
    safeLinksMessage = $(safeLinksMessage, "Test links message");
    data = $(data, createError("Test Error", { status: 500 }));

    errorInfos = errorInfos.reduce((result, ei) => ({
      ...result,
      [ei.statusCode]: { title: ei.title, message: ei.message}
    }), {});

    return {
      type: "ErrorPage",
      schema: { errorInfos, footerInfo, safeLinks, safeLinksMessage},
      data: { type: "Inline", value: data }
    };
  },

  anErrorPageInfo({ statusCode, title, message } = {}){
    statusCode = $(statusCode, "500");
    title = $(title, "Test Title");
    message = $(message, "Test message");

    return {
      statusCode,
      title,
      message
    };
  },

  anExpected({ testId, value, data } = {}) {
    testId = $(testId, "test-id");
    value = $(value, "");
    data = $(data, { type: "Inline", value });

    return {
      type: "Expected",
      schema: testId,
      data
    };
  },

  aHeader({ sitemapItems, data } = {}) {
    sitemapItems = $(sitemapItems, [this.aHeaderItem({ route: "/test" })]);
    data = $(data, { path: "/test" });

    return {
      type: "Header",
      schema: {
        sitemap: sitemapItems
          .reduce((result, item) => ({
            ...result,
            [item.route]: item
          }), {})
      },
      data: { type: "Inline", value: data }
    };
  },

  aHeaderItem({ route, icon, title, parentRoute } = {}) {
    route = $(route, "/item");
    icon = $(icon, route === "/" ? "pi pi-home" : "pi pi-heart");

    return { route, icon, title, parentRoute };
  },

  anIcon({ iconClass } = {}) {
    iconClass = $(iconClass, "pi-heart");

    return {
      type: "Icon",
      schema: { iconClass }
    };
  },

  anInput({ testId } = {}) {
    testId = $(testId, "test-input");

    return {
      type: "Input",
      schema: testId
    };
  },

  aLink({ path, idProp, textProp, data } = {}) {
    path = $(path, "/some-object/{0}");
    idProp = $(idProp, "id");
    textProp = $(textProp, "name");
    data = $(data, { id: "test-id", name: "Test" });

    return {
      type: "Link",
      schema: { path, idProp, textProp },
      data: { type: "Inline", value: data }
    };
  },

  aMoney({ data } = {}) {
    data = $(data, 100_000);

    return {
      type: "Money",
      data: { type: "Inline", value: data }
    };
  },

  aMenuPage({ header, links } = {}) {
    header = $(header, this.anExpected());
    links = $(links, []);

    return {
      type: "MenuPage",
      schema: { header, links }
    };
  },

  aPageTitle({ title, description, actions } = {}) {
    title = $(title, "Test Title");
    description = $(description, "Test description is given for testing purposes");
    actions = $(actions, []);

    return {
      type: "PageTitle",
      schema: { title, description, actions }
    };
  },

  aParameter({ name, component, required, defaultValue } = {}) {
    name = $(name, "test");
    required = $(required, false);
    component = $(component, this.anInput());

    return { name, required, default: defaultValue, component };
  },

  theQueryData() {
    return {
      type: "Computed",
      composable: "useQuery"
    };
  },

  aRate({ data } = {}) {
    data = $(data, 0.5);

    return {
      type: "Rate",
      data: { type: "Inline", value: data }
    };
  },

  aReportPage({ title, description, queryParameters, tabs } = {}) {
    title = this.aPageTitle({ title, description }).schema;
    queryParameters = $(queryParameters, []);
    tabs = $(tabs, [this.aReportPageTab()]);

    return {
      type: "ReportPage",
      schema: { title, queryParameters, tabs }
    };
  },

  aReportPageTab({ id, title, icon, contents } = {}) {
    id = $(id, "test-tab");
    title = $(title, "Test Tab");
    icon = $(icon, this.anIcon());
    contents = $(contents, [this.aReportPageTabContent()]);

    return { id, title, icon, contents };
  },

  aReportPageTabContent({ component, fullScreen, narrow } = {}) {
    component = $(component, this.anExpected({ value: "Test content is given for testing purposes" }));
    fullScreen = $(fullScreen, false);
    narrow = $(narrow, false);

    return { component, fullScreen, narrow };
  },

  aSelect({ label, optionLabel, optionValue, showClear, data } = {}) {
    label = $(label, "Test");
    showClear = $(showClear, false);
    data = $(data, ["Test Option 1", "Test Option 2"]);

    return {
      type: "Select",
      schema: { label, optionLabel, optionValue, showClear },
      data: { type: "Inline", value: data }
    };
  },

  aSideMenu({ logo, menu, data, footer } = {}) {
    logo = $(logo, "logo.svg");
    menu = $(menu, []);
    data = $(data, { path: "/test" });
    footer = $(footer, this.anExpected());

    return {
      type: "SideMenu",
      schema: { logo, menu, footer },
      data: { type: "Inline", value: data }
    };
  },

  aSideMenuItem({ route, icon, title, disabled } = {}) {
    route = $(route, "/item");
    icon = $(icon, "pi pi-home");
    disabled = $(disabled, false);

    return { route, icon, title, disabled };
  }
};
