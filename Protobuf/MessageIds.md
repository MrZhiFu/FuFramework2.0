# 消息 ID 速查表

> 由 ProtoExport 自动生成（勿手改）。消息 ID = (module << 16) + opcode。

## Bag（module 100）

| 消息名 | opcode | 完整 ID |
|---|---|---|
| ReqBagInfo | 10 | 6553610 |
| RespBagInfo | 11 | 6553611 |
| ReqUseItem | 12 | 6553612 |
| RespUseItem | 13 | 6553613 |
| ReqAddItem | 14 | 6553614 |
| RespAddItem | 15 | 6553615 |
| ReqRemoveItem | 16 | 6553616 |
| RespRemoveItem | 17 | 6553617 |
| NotifyBagItem | 18 | 6553618 |
| NotifyBagInfoChanged | 19 | 6553619 |

## Basic（module 10）

| 消息名 | opcode | 完整 ID |
|---|---|---|
| ReqHeartBeat | 10 | 655370 |
| NotifyHeartBeat | 11 | 655371 |
| NotifyServerFullyLoaded | 12 | 655372 |

## Common（module 20）

| 消息名 | opcode | 完整 ID |
|---|---|---|

## Inner_Basic（module 2）

| 消息名 | opcode | 完整 ID |
|---|---|---|

## User（module 300）

| 消息名 | opcode | 完整 ID |
|---|---|---|
| ReqUserLogin | 10 | 19660810 |
| RespUserLogin | 11 | 19660811 |
| ReqPlayerCreate | 12 | 19660812 |
| RespPlayerCreate | 13 | 19660813 |
| ReqPlayerList | 14 | 19660814 |
| RespPlayerList | 15 | 19660815 |
| ReqPlayerLogin | 16 | 19660816 |
| RespPlayerLogin | 17 | 19660817 |
| NotifyErrorCode | 18 | 19660818 |
| NotifyPrompt | 19 | 19660819 |

> 盲区：module 32766（short.MaxValue-1）段为服务器内置协议 DiscoveryCenter，手写维护，不在本表生成范围。
