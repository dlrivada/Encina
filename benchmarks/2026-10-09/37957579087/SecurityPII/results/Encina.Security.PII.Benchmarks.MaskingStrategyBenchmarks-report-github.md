```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  76.51 ns |   4.711 ns |  3.116 ns |  1.00 |    0.05 | 0.0026 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 304.07 ns |   5.695 ns |  2.979 ns |  3.98 |    0.15 | 0.0062 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 363.70 ns |   6.069 ns |  4.014 ns |  4.76 |    0.19 | 0.0062 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 287.54 ns |   1.933 ns |  1.011 ns |  3.76 |    0.14 | 0.0062 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 132.07 ns |   1.494 ns |  0.988 ns |  1.73 |    0.07 | 0.0033 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     |  85.18 ns |   3.653 ns |  2.416 ns |  1.11 |    0.05 | 0.0038 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 178.68 ns |  12.399 ns |  8.201 ns |  2.34 |    0.14 | 0.0045 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 128.84 ns |   3.578 ns |  1.871 ns |  1.69 |    0.07 | 0.0031 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  43.60 ns |   0.653 ns |  0.388 ns |  0.57 |    0.02 | 0.0015 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  50.55 ns |   0.753 ns |  0.394 ns |  0.66 |    0.03 | 0.0015 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     |  81.25 ns |   3.100 ns |  1.844 ns |  1.06 |    0.05 | 0.0038 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 414.78 ns |   3.214 ns |  1.913 ns |  5.43 |    0.21 | 0.0043 |     392 B |        1.75 |
|                     |            |                |             |           |            |           |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  75.43 ns |  11.895 ns |  0.652 ns |  1.00 |    0.01 | 0.0026 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 296.52 ns |  48.425 ns |  2.654 ns |  3.93 |    0.04 | 0.0062 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 358.39 ns |   9.237 ns |  0.506 ns |  4.75 |    0.04 | 0.0062 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 314.85 ns | 386.843 ns | 21.204 ns |  4.17 |    0.25 | 0.0062 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 138.59 ns | 172.594 ns |  9.460 ns |  1.84 |    0.11 | 0.0033 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           |  81.68 ns |  28.567 ns |  1.566 ns |  1.08 |    0.02 | 0.0038 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 170.50 ns |  25.943 ns |  1.422 ns |  2.26 |    0.02 | 0.0045 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 126.88 ns |   0.861 ns |  0.047 ns |  1.68 |    0.01 | 0.0031 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  44.74 ns |   5.702 ns |  0.313 ns |  0.59 |    0.01 | 0.0015 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  52.53 ns |   4.886 ns |  0.268 ns |  0.70 |    0.01 | 0.0015 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           |  82.97 ns |  40.608 ns |  2.226 ns |  1.10 |    0.03 | 0.0038 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 412.04 ns |  10.949 ns |  0.600 ns |  5.46 |    0.04 | 0.0043 |     392 B |        1.75 |
