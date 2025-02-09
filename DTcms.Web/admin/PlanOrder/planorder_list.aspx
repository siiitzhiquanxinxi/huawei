﻿<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="planorder_list.aspx.cs" Inherits="DTcms.Web.admin.PlanOrder.planorder_list" %>
<%@ Register Assembly="AspNetPager" Namespace="Wuqi.Webdiyer" TagPrefix="webdiyer" %>
<%@ Import namespace="DTcms.Common" %>
<!DOCTYPE html>

<html>
<head>
<meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
<meta name="viewport" content="width=device-width,minimum-scale=1.0,maximum-scale=1.0,initial-scale=1.0,user-scalable=no" />
<meta name="apple-mobile-web-app-capable" content="yes" />
<title>排产计划查询列表</title>
<link href="../../scripts/artdialog/ui-dialog.css" rel="stylesheet" type="text/css" />
<link href="../skin/default/style.css" rel="stylesheet" type="text/css" />
<link href="../../css/pagination.css" rel="stylesheet" type="text/css" />
<script type="text/javascript" src="../../scripts/jquery/jquery-1.11.2.min.js"></script>
<script type="text/javascript" src="../../scripts/artdialog/dialog-plus-min.js"></script>
<script type="text/javascript" charset="utf-8" src="../js/laymain.js"></script>
<script type="text/javascript" charset="utf-8" src="../js/common.js"></script>
<script type="text/javascript" src="../../scripts/datepicker/WdatePicker.js"></script>
</head>

<body class="mainbody">
<form id="form1" runat="server">
<!--导航栏-->
<div class="location">
  <a href="javascript:history.back(-1);" class="back"><i></i><span>返回上一页</span></a>
  <a href="../center.aspx" class="home"><i></i><span>首页</span></a>
  <i class="arrow"></i>
  <span>排产计划查询列表</span>
</div>
<!--/导航栏-->

<!--工具栏-->
<div id="floatHead" class="toolbar-wrap">
  <div class="toolbar">
    <div class="box-wrap">
      <a class="menu-btn"></a>
      <div class="l-list">
        <ul class="icon-list">
        </ul>
      </div>
      <div >
        <%--<asp:TextBox ID="txtKeywords" runat="server" CssClass="keyword" />
        <asp:LinkButton ID="lbtnSearch" runat="server" CssClass="btn-search" onclick="btnSearch_Click">查询</asp:LinkButton>--%>
          <asp:Label ID="Label2" runat="server" Text="零件号" style="padding-left:10px;vertical-align:middle;"></asp:Label>
                        <asp:TextBox ID="txtPartNum" runat="server"   CssClass="input"/>
          <asp:Label ID="Label7" runat="server" Text="计划开工日期" style="padding-left:10px;"></asp:Label>
                        <asp:TextBox ID="txtSDate" runat="server" CssClass="input date" onfocus="WdatePicker({dateFmt:'yyyy-MM-dd'})"
                    datatype="*1-50" errormsg="请选择正确的日期" sucmsg=" " nullmsg=" " />——
                        <asp:TextBox ID="txtEDate" runat="server" CssClass="input date" onfocus="WdatePicker({dateFmt:'yyyy-MM-dd'})"
                    datatype="*1-50" errormsg="请选择正确的日期" sucmsg=" " nullmsg=" " />
           <asp:Label ID="Label6" runat="server" Text="状态" style="padding-right:10px;padding-left:10px;"></asp:Label>
                        <asp:DropDownList ID="ddlState" runat="server"  CssClass="input">
                            <asp:ListItem></asp:ListItem>
                            <asp:ListItem Value="0">待备刀</asp:ListItem>
                            <asp:ListItem Value="1">备刀中</asp:ListItem>
                            <asp:ListItem Value="2">已完成</asp:ListItem>
                            <asp:ListItem Value="-1">异常</asp:ListItem>
                            <asp:ListItem Value="-2">已取消</asp:ListItem>
                        </asp:DropDownList>
          <asp:Label ID="Label3" runat="server" Text="是否导入CAM" style="padding-right:10px;padding-left:10px;"></asp:Label>
                        <asp:DropDownList ID="ddlCAM" runat="server"  CssClass="input">
                            <asp:ListItem Value="0">未导入</asp:ListItem>
                            <asp:ListItem Value="1">已导入</asp:ListItem>
                            <asp:ListItem Value="2">全部</asp:ListItem>
                        </asp:DropDownList>
                        <asp:Button ID="btnSearch" runat="server" Text="查询" OnClick="btnSearch_Click" style="padding-right:25px;padding-left:25px; margin-left:10px;" CssClass="btn"/>
      </div>
    </div>
  </div>
</div>
<!--/工具栏-->

<!--列表-->
<div class="table-container">
  <asp:Repeater ID="rptList" runat="server">
  <HeaderTemplate>
  <table width="100%" border="0" cellspacing="0" cellpadding="0" class="ltable">
    <tr>
      <%--<th width="8%">选择</th>--%>
      <th align="center" width="10%">计划号</th>
      <th align="center" width="10%">工装号</th> 
      <th align="center" width="10%">零件号</th>
      <th align="center" width="10%">零件名称</th>
      <th align="center" width="10%">材质</th>
      <th align="center" width="10%">计划开工时间</th>
      <th align="center" width="10%">备刀时间延期至</th>
      <th align="center" width="10%">机台</th> 
      <th align="center" width="10%">加工夹位</th>
      <th align="center" width="5%">备刀状态</th> 
      <th width="8%">操作</th>
    </tr>
  </HeaderTemplate>
  <ItemTemplate>
    <tr>
      <%--<td align="center">
        <asp:CheckBox ID="chkId" CssClass="checkall" runat="server" style="vertical-align:middle;" />
        <asp:HiddenField ID="hidId" Value='<%#Eval("BatchNumber")%>' runat="server" />
      </td>--%>
      <td align="center"><%# Eval("PlanNo") %></td>
      <td align="center"><%# Eval("ComponentNo") %></td>
      <td align="center"><%# Eval("PartNum") %></td>
      <td align="center"><%# Eval("PartName") %></td>
      <td align="center"><%# Eval("MaterialTexture") %></td>
      <td align="center"><%# Eval("PlanWorkTime") %></td>
        <td align="center"><%# Eval("DelayWorkTime") %></td>
        <td align="center"><%# Eval("MachineLathe") %></td>
        <td align="center"><%# Eval("WorkProcedure") %></td>
      <td align="center"><%# Eval("OrderReadyState").ToString()=="0"?"待备刀" : Eval("OrderReadyState").ToString()=="1"?"备刀中" : Eval("OrderReadyState").ToString()=="2"?"已完成":  Eval("OrderReadyState").ToString()=="-1"?"异常":"已取消" %></td>
      <td align="center">
          <a href="planorder_view.aspx?action=<%#DTEnums.ActionEnum.Edit %>&id=<%#Eval("id")%>">查看</a>
              <%--<asp:LinkButton ID="lbtnExport" runat="server" CommandArgument='<%#Eval("BatchNumber") %>' OnClick="lbtnExport_Click">导出</asp:LinkButton>
              <asp:LinkButton ID="lbtnDel" runat="server" CommandArgument='<%#Eval("BatchNumber") %>' OnClick="lbtnDel_Click">删除</asp:LinkButton>--%>
      </td>
        
    </tr>
  </ItemTemplate>
  <FooterTemplate>
    <%#rptList.Items.Count == 0 ? "<tr><td align=\"center\" colspan=\"11\">暂无记录</td></tr>" : ""%>
  </table>
  </FooterTemplate>
  </asp:Repeater>
</div>
<!--/列表-->

<!--内容底部-->
<div class="line20"></div>
<webdiyer:AspNetPager ID="AspNetPager1" runat="server" CustomInfoHTML="当前页:%CurrentPageIndex%/%PageCount% 共有%RecordCount%条记录 %PageCount%/页"
            FirstPageText="首页" HorizontalAlign="Center" InvalidPageIndexErrorMessage="页索引不是有效的数值！"
            LastPageText="末页" NextPageText="下一页" PageIndexOutOfRangeErrorMessage="页索引超出范围！"
            PageSize="20" PrevPageText="上一页" ShowCustomInfoSection="Left" ShowInputBox="Always"
            Width="100%" OnPageChanged="AspNetPager1_PageChanged" NumericButtonCount="5">
        </webdiyer:AspNetPager>
<!--/内容底部-->
</form>
</body>
</html>

