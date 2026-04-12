```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method              | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error    | StdDev   | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |------------ |----------:|---------:|---------:|----------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     | 3           | 102.00 ns | 1.212 ns | 0.634 ns | 102.05 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 3           | 426.44 ns | 2.374 ns | 1.570 ns | 426.38 ns |  4.18 |    0.03 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 3           | 494.07 ns | 3.722 ns | 2.215 ns | 493.85 ns |  4.84 |    0.04 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 3           | 414.36 ns | 4.882 ns | 3.229 ns | 414.53 ns |  4.06 |    0.04 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 3           | 184.42 ns | 1.491 ns | 0.887 ns | 184.06 ns |  1.81 |    0.01 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 3           | 128.63 ns | 2.429 ns | 1.445 ns | 128.51 ns |  1.26 |    0.02 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 3           | 227.63 ns | 2.418 ns | 1.439 ns | 227.17 ns |  2.23 |    0.02 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 3           | 185.73 ns | 1.226 ns | 0.730 ns | 185.64 ns |  1.82 |    0.01 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     | 3           |  57.17 ns | 0.750 ns | 0.496 ns |  57.37 ns |  0.56 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     | 3           |  73.44 ns | 0.848 ns | 0.561 ns |  73.41 ns |  0.72 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 3           | 110.69 ns | 2.353 ns | 1.557 ns | 110.26 ns |  1.09 |    0.02 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 3           | 610.36 ns | 5.882 ns | 3.500 ns | 608.72 ns |  5.98 |    0.05 | 0.0248 |     416 B |        1.86 |
|                     |            |                |             |             |           |          |          |           |       |         |        |           |             |
| Email_Partial       | MediumRun  | 15             | 2           | 10          | 101.56 ns | 1.073 ns | 1.607 ns | 102.11 ns |  1.00 |    0.02 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | MediumRun  | 15             | 2           | 10          | 424.73 ns | 3.268 ns | 4.790 ns | 425.89 ns |  4.18 |    0.08 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | MediumRun  | 15             | 2           | 10          | 497.02 ns | 3.430 ns | 5.028 ns | 497.38 ns |  4.90 |    0.09 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | MediumRun  | 15             | 2           | 10          | 415.14 ns | 3.749 ns | 5.131 ns | 412.27 ns |  4.09 |    0.08 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | MediumRun  | 15             | 2           | 10          | 191.32 ns | 0.637 ns | 0.894 ns | 191.54 ns |  1.88 |    0.03 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | MediumRun  | 15             | 2           | 10          | 129.62 ns | 0.333 ns | 0.499 ns | 129.62 ns |  1.28 |    0.02 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | MediumRun  | 15             | 2           | 10          | 236.08 ns | 2.686 ns | 4.020 ns | 237.37 ns |  2.33 |    0.05 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | MediumRun  | 15             | 2           | 10          | 184.57 ns | 2.043 ns | 3.058 ns | 185.41 ns |  1.82 |    0.04 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | MediumRun  | 15             | 2           | 10          |  56.42 ns | 0.294 ns | 0.422 ns |  56.41 ns |  0.56 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | MediumRun  | 15             | 2           | 10          |  72.12 ns | 0.236 ns | 0.338 ns |  72.10 ns |  0.71 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | MediumRun  | 15             | 2           | 10          | 112.18 ns | 1.047 ns | 1.567 ns | 112.26 ns |  1.10 |    0.02 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | MediumRun  | 15             | 2           | 10          | 619.79 ns | 1.335 ns | 1.957 ns | 619.15 ns |  6.10 |    0.10 | 0.0248 |     416 B |        1.86 |
