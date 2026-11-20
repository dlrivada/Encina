```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9974)
13th Gen Intel Core i9-13900KS 3.20GHz, 1 CPU, 32 logical and 24 physical cores
.NET SDK 10.0.404
  [Host] : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v3

Job=ShortRun  Toolchain=InProcessEmitToolchain  IterationCount=3  
LaunchCount=1  WarmupCount=3  

```
| Method                     | Categories     | Mean       | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |--------------- |-----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Either_AsyncFailure        | Async_failure  | 77.5526 ns | 56.1778 ns | 3.0793 ns |  1.00 |    0.05 |      - |         - |          NA |
| StdUnion_AsyncFailure      | Async_failure  | 30.8849 ns | 33.3908 ns | 1.8303 ns |  0.40 |    0.02 | 0.0030 |      56 B |          NA |
| Custom_AsyncFailure        | Async_failure  | 71.1305 ns | 92.4836 ns | 5.0693 ns |  0.92 |    0.07 |      - |         - |          NA |
| Hierarchy_AsyncFailure     | Async_failure  | 28.8678 ns | 48.2620 ns | 2.6454 ns |  0.37 |    0.03 | 0.0030 |      56 B |          NA |
| BoxError_AsyncFailure      | Async_failure  | 39.7070 ns | 13.7729 ns | 0.7549 ns |  0.51 |    0.02 | 0.0029 |      56 B |          NA |
| RefError_AsyncFailure      | Async_failure  | 29.2929 ns |  2.5849 ns | 0.1417 ns |  0.38 |    0.01 |      - |         - |          NA |
| SlimError_AsyncFailure     | Async_failure  | 51.6385 ns | 47.6362 ns | 2.6111 ns |  0.67 |    0.04 |      - |         - |          NA |
|                            |                |            |            |           |       |         |        |           |             |
| Either_Async               | Async_success  | 73.1858 ns | 42.8537 ns | 2.3490 ns |  1.00 |    0.04 |      - |         - |          NA |
| StdUnion_Async             | Async_success  | 20.6745 ns | 21.4990 ns | 1.1784 ns |  0.28 |    0.02 | 0.0013 |      24 B |          NA |
| Custom_Async               | Async_success  | 65.9773 ns | 37.9267 ns | 2.0789 ns |  0.90 |    0.03 |      - |         - |          NA |
| Hierarchy_Async            | Async_success  | 16.0733 ns | 56.7400 ns | 3.1101 ns |  0.22 |    0.04 | 0.0013 |      24 B |          NA |
| BoxError_Async             | Async_success  | 28.5945 ns | 20.8015 ns | 1.1402 ns |  0.39 |    0.02 |      - |         - |          NA |
| RefError_Async             | Async_success  | 31.7641 ns |  9.5811 ns | 0.5252 ns |  0.43 |    0.01 |      - |         - |          NA |
| SlimError_Async            | Async_success  | 50.7888 ns | 42.0915 ns | 2.3072 ns |  0.69 |    0.03 |      - |         - |          NA |
|                            |                |            |            |           |       |         |        |           |             |
| Either_Chain3              | Chain3         | 34.1708 ns | 20.6711 ns | 1.1331 ns |  1.00 |    0.04 | 0.0013 |      24 B |        1.00 |
| StdUnion_Chain3            | Chain3         | 17.6218 ns | 18.4712 ns | 1.0125 ns |  0.52 |    0.03 | 0.0051 |      96 B |        4.00 |
| Custom_Chain3              | Chain3         | 30.3355 ns | 15.0928 ns | 0.8273 ns |  0.89 |    0.03 |      - |         - |        0.00 |
| Hierarchy_Chain3           | Chain3         | 18.4984 ns | 37.7624 ns | 2.0699 ns |  0.54 |    0.05 | 0.0051 |      96 B |        4.00 |
| BoxError_Chain3            | Chain3         | 10.8121 ns |  6.3738 ns | 0.3494 ns |  0.32 |    0.01 |      - |         - |        0.00 |
| RefError_Chain3            | Chain3         |  7.5704 ns |  6.8732 ns | 0.3767 ns |  0.22 |    0.01 |      - |         - |        0.00 |
| SlimError_Chain3           | Chain3         | 26.0727 ns | 19.2147 ns | 1.0532 ns |  0.76 |    0.03 |      - |         - |        0.00 |
|                            |                |            |            |           |       |         |        |           |             |
| Either_Chain3Failure       | Chain3_failure | 48.6765 ns | 27.9853 ns | 1.5340 ns |  1.00 |    0.04 | 0.0013 |      24 B |        1.00 |
| StdUnion_Chain3Failure     | Chain3_failure | 50.0617 ns | 42.8608 ns | 2.3493 ns |  1.03 |    0.05 | 0.0119 |     224 B |        9.33 |
| Custom_Chain3Failure       | Chain3_failure | 59.4303 ns | 17.4500 ns | 0.9565 ns |  1.22 |    0.04 |      - |         - |        0.00 |
| Hierarchy_Chain3Failure    | Chain3_failure | 32.0917 ns | 16.8086 ns | 0.9213 ns |  0.66 |    0.02 | 0.0089 |     168 B |        7.00 |
| BoxError_Chain3Failure     | Chain3_failure | 19.4079 ns | 43.3702 ns | 2.3773 ns |  0.40 |    0.04 | 0.0030 |      56 B |        2.33 |
| RefError_Chain3Failure     | Chain3_failure | 18.0002 ns | 26.8968 ns | 1.4743 ns |  0.37 |    0.03 |      - |         - |        0.00 |
| SlimError_Chain3Failure    | Chain3_failure | 36.6665 ns | 31.9178 ns | 1.7495 ns |  0.75 |    0.04 |      - |         - |        0.00 |
|                            |                |            |            |           |       |         |        |           |             |
| Either_Failure             | Failure        | 10.8736 ns |  9.2524 ns | 0.5072 ns |  1.00 |    0.06 |      - |         - |          NA |
| StdUnion_Failure           | Failure        |  9.9716 ns | 26.7840 ns | 1.4681 ns |  0.92 |    0.12 | 0.0030 |      56 B |          NA |
| Custom_Failure             | Failure        |  9.3122 ns | 22.9980 ns | 1.2606 ns |  0.86 |    0.11 |      - |         - |          NA |
| Hierarchy_Failure          | Failure        | 13.9045 ns | 62.4748 ns | 3.4245 ns |  1.28 |    0.28 | 0.0030 |      56 B |          NA |
| BoxError_Failure           | Failure        |  9.7365 ns | 48.2644 ns | 2.6455 ns |  0.90 |    0.21 | 0.0030 |      56 B |          NA |
| RefError_Failure           | Failure        |  1.1813 ns |  3.1850 ns | 0.1746 ns |  0.11 |    0.01 |      - |         - |          NA |
| RefError_FailureFreshError | Failure        |  6.2890 ns | 15.4622 ns | 0.8475 ns |  0.58 |    0.07 | 0.0030 |      56 B |          NA |
| SlimError_Failure          | Failure        |  1.7798 ns |  1.2006 ns | 0.0658 ns |  0.16 |    0.01 |      - |         - |          NA |
|                            |                |            |            |           |       |         |        |           |             |
| Either_Success             | Success_int    |  9.1371 ns |  4.0071 ns | 0.2196 ns |  1.00 |    0.03 |      - |         - |          NA |
| StdUnion_Success           | Success_int    |  5.3805 ns | 21.0864 ns | 1.1558 ns |  0.59 |    0.11 | 0.0013 |      24 B |          NA |
| Custom_Success             | Success_int    |  8.8064 ns |  4.8619 ns | 0.2665 ns |  0.96 |    0.03 |      - |         - |          NA |
| Hierarchy_Success          | Success_int    |  4.8065 ns | 19.0094 ns | 1.0420 ns |  0.53 |    0.10 | 0.0013 |      24 B |          NA |
| BoxError_Success           | Success_int    |  2.5651 ns |  1.7492 ns | 0.0959 ns |  0.28 |    0.01 |      - |         - |          NA |
| RefError_Success           | Success_int    |  2.7998 ns |  2.9689 ns | 0.1627 ns |  0.31 |    0.02 |      - |         - |          NA |
| SlimError_Success          | Success_int    |  7.8344 ns | 13.8693 ns | 0.7602 ns |  0.86 |    0.07 |      - |         - |          NA |
|                            |                |            |            |           |       |         |        |           |             |
| Either_SuccessRef          | Success_ref    |  8.4455 ns |  3.3334 ns | 0.1827 ns |  1.00 |    0.03 |      - |         - |          NA |
| StdUnion_SuccessRef        | Success_ref    |  0.8808 ns |  5.6119 ns | 0.3076 ns |  0.10 |    0.03 |      - |         - |          NA |
| Custom_SuccessRef          | Success_ref    |  5.5356 ns |  3.5182 ns | 0.1928 ns |  0.66 |    0.02 |      - |         - |          NA |
| Hierarchy_SuccessRef       | Success_ref    |  4.2329 ns | 13.5562 ns | 0.7431 ns |  0.50 |    0.08 | 0.0013 |      24 B |          NA |
| BoxError_SuccessRef        | Success_ref    |  0.7739 ns |  1.9018 ns | 0.1042 ns |  0.09 |    0.01 |      - |         - |          NA |
| RefError_SuccessRef        | Success_ref    |  0.5404 ns |  4.1169 ns | 0.2257 ns |  0.06 |    0.02 |      - |         - |          NA |
| SlimError_SuccessRef       | Success_ref    |  5.9749 ns |  8.8732 ns | 0.4864 ns |  0.71 |    0.05 |      - |         - |          NA |
