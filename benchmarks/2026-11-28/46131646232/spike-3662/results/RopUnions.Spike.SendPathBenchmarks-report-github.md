```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9818)
13th Gen Intel Core i9-13900KS 3.20GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 11.0.100
  [Host] : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v3

Job=ShortRun  Toolchain=InProcessEmitToolchain  IterationCount=3  
LaunchCount=1  WarmupCount=3  

```
| Method                  | Categories     | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------ |--------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Either_AsyncFailure     | Async_failure  | 66.0336 ns | 14.7314 ns | 0.8075 ns |  1.00 |    0.01 |      - |         - |          NA |
| StdUnion_AsyncFailure   | Async_failure  | 19.4130 ns | 18.6904 ns | 1.0245 ns |  0.29 |    0.01 | 0.0030 |      56 B |          NA |
| Custom_AsyncFailure     | Async_failure  | 64.1807 ns | 53.8201 ns | 2.9501 ns |  0.97 |    0.04 |      - |         - |          NA |
| Hierarchy_AsyncFailure  | Async_failure  | 16.6880 ns |  3.6835 ns | 0.2019 ns |  0.25 |    0.00 | 0.0030 |      56 B |          NA |
|                         |                |            |            |           |       |         |        |           |             |
| Either_Async            | Async_success  | 62.2017 ns |  5.3862 ns | 0.2952 ns |  1.00 |    0.01 |      - |         - |          NA |
| StdUnion_Async          | Async_success  | 13.1481 ns |  1.0080 ns | 0.0553 ns |  0.21 |    0.00 | 0.0013 |      24 B |          NA |
| Custom_Async            | Async_success  | 61.1057 ns |  1.1598 ns | 0.0636 ns |  0.98 |    0.00 |      - |         - |          NA |
| Hierarchy_Async         | Async_success  | 12.6685 ns |  6.9511 ns | 0.3810 ns |  0.20 |    0.01 | 0.0013 |      24 B |          NA |
|                         |                |            |            |           |       |         |        |           |             |
| Either_Chain3           | Chain3         | 32.0494 ns |  2.6870 ns | 0.1473 ns |  1.00 |    0.01 | 0.0013 |      24 B |        1.00 |
| StdUnion_Chain3         | Chain3         | 12.7686 ns |  1.5362 ns | 0.0842 ns |  0.40 |    0.00 | 0.0051 |      96 B |        4.00 |
| Custom_Chain3           | Chain3         | 29.5523 ns |  3.8817 ns | 0.2128 ns |  0.92 |    0.01 |      - |         - |        0.00 |
| Hierarchy_Chain3        | Chain3         | 12.4734 ns | 11.5800 ns | 0.6347 ns |  0.39 |    0.02 | 0.0051 |      96 B |        4.00 |
|                         |                |            |            |           |       |         |        |           |             |
| Either_Chain3Failure    | Chain3_failure | 45.8893 ns |  3.6058 ns | 0.1976 ns |  1.00 |    0.01 | 0.0013 |      24 B |        1.00 |
| StdUnion_Chain3Failure  | Chain3_failure | 44.7865 ns | 50.5071 ns | 2.7685 ns |  0.98 |    0.05 | 0.0119 |     224 B |        9.33 |
| Custom_Chain3Failure    | Chain3_failure | 48.5506 ns |  6.9592 ns | 0.3815 ns |  1.06 |    0.01 |      - |         - |        0.00 |
| Hierarchy_Chain3Failure | Chain3_failure | 21.5681 ns | 16.4790 ns | 0.9033 ns |  0.47 |    0.02 | 0.0059 |     112 B |        4.67 |
|                         |                |            |            |           |       |         |        |           |             |
| Either_Failure          | Failure        |  9.4937 ns |  0.5579 ns | 0.0306 ns |  1.00 |    0.00 |      - |         - |          NA |
| StdUnion_Failure        | Failure        |  7.1052 ns |  3.6096 ns | 0.1979 ns |  0.75 |    0.02 | 0.0030 |      56 B |          NA |
| Custom_Failure          | Failure        |  6.6336 ns |  0.8098 ns | 0.0444 ns |  0.70 |    0.00 |      - |         - |          NA |
| Hierarchy_Failure       | Failure        |  6.0556 ns |  2.8843 ns | 0.1581 ns |  0.64 |    0.01 | 0.0030 |      56 B |          NA |
|                         |                |            |            |           |       |         |        |           |             |
| Either_Success          | Success_int    |  6.4777 ns |  0.6933 ns | 0.0380 ns |  1.00 |    0.01 |      - |         - |          NA |
| StdUnion_Success        | Success_int    |  3.8614 ns |  1.6296 ns | 0.0893 ns |  0.60 |    0.01 | 0.0013 |      24 B |          NA |
| Custom_Success          | Success_int    |  7.1925 ns |  0.2999 ns | 0.0164 ns |  1.11 |    0.01 |      - |         - |          NA |
| Hierarchy_Success       | Success_int    |  2.9547 ns |  2.3501 ns | 0.1288 ns |  0.46 |    0.02 | 0.0013 |      24 B |          NA |
|                         |                |            |            |           |       |         |        |           |             |
| Either_SuccessRef       | Success_ref    |  7.2618 ns |  8.4590 ns | 0.4637 ns |  1.00 |    0.08 |      - |         - |          NA |
| StdUnion_SuccessRef     | Success_ref    |  0.7316 ns |  0.0466 ns | 0.0026 ns |  0.10 |    0.01 |      - |         - |          NA |
| Custom_SuccessRef       | Success_ref    |  6.0956 ns |  0.3699 ns | 0.0203 ns |  0.84 |    0.05 |      - |         - |          NA |
| Hierarchy_SuccessRef    | Success_ref    |  4.5677 ns |  5.0991 ns | 0.2795 ns |  0.63 |    0.05 | 0.0013 |      24 B |          NA |
