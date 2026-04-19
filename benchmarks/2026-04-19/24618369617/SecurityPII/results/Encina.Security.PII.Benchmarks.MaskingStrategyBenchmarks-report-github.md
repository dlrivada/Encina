```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]     : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3


```
| Method              | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |------------ |----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     | 3           | 101.94 ns | 1.447 ns | 0.957 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 3           | 425.36 ns | 7.375 ns | 4.878 ns |  4.17 |    0.06 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 3           | 485.44 ns | 3.214 ns | 2.126 ns |  4.76 |    0.05 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 3           | 399.90 ns | 5.155 ns | 3.067 ns |  3.92 |    0.05 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 3           | 186.61 ns | 2.250 ns | 1.488 ns |  1.83 |    0.02 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 3           | 124.70 ns | 1.722 ns | 1.139 ns |  1.22 |    0.02 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 3           | 232.93 ns | 5.801 ns | 3.837 ns |  2.29 |    0.04 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 3           | 188.21 ns | 1.291 ns | 0.854 ns |  1.85 |    0.02 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     | 3           |  58.25 ns | 0.756 ns | 0.500 ns |  0.57 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     | 3           |  72.01 ns | 1.057 ns | 0.699 ns |  0.71 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 3           | 103.10 ns | 4.572 ns | 2.720 ns |  1.01 |    0.03 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 3           | 588.41 ns | 9.154 ns | 5.447 ns |  5.77 |    0.07 | 0.0248 |     416 B |        1.86 |
|                     |            |                |             |             |           |          |          |       |         |        |           |             |
| Email_Partial       | MediumRun  | 15             | 2           | 10          |  94.46 ns | 0.664 ns | 0.974 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | MediumRun  | 15             | 2           | 10          | 408.07 ns | 2.469 ns | 3.619 ns |  4.32 |    0.06 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | MediumRun  | 15             | 2           | 10          | 495.49 ns | 5.287 ns | 7.913 ns |  5.25 |    0.10 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | MediumRun  | 15             | 2           | 10          | 394.44 ns | 2.103 ns | 3.147 ns |  4.18 |    0.05 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | MediumRun  | 15             | 2           | 10          | 175.98 ns | 0.677 ns | 0.972 ns |  1.86 |    0.02 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | MediumRun  | 15             | 2           | 10          | 118.05 ns | 0.943 ns | 1.353 ns |  1.25 |    0.02 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | MediumRun  | 15             | 2           | 10          | 220.97 ns | 1.898 ns | 2.782 ns |  2.34 |    0.04 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | MediumRun  | 15             | 2           | 10          | 178.80 ns | 0.732 ns | 1.026 ns |  1.89 |    0.02 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | MediumRun  | 15             | 2           | 10          |  53.30 ns | 0.298 ns | 0.437 ns |  0.56 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | MediumRun  | 15             | 2           | 10          |  68.92 ns | 0.583 ns | 0.855 ns |  0.73 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | MediumRun  | 15             | 2           | 10          | 101.35 ns | 0.651 ns | 0.892 ns |  1.07 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | MediumRun  | 15             | 2           | 10          | 584.40 ns | 2.001 ns | 2.870 ns |  6.19 |    0.07 | 0.0248 |     416 B |        1.86 |
