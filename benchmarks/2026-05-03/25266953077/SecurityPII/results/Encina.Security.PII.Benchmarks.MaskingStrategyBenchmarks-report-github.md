```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]     : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3


```
| Method              | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     | 3           |  97.21 ns |  4.373 ns |  2.893 ns |  97.40 ns |  1.00 |    0.04 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 3           | 431.15 ns | 10.712 ns |  7.085 ns | 430.77 ns |  4.44 |    0.14 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 3           | 510.99 ns |  4.543 ns |  2.704 ns | 511.05 ns |  5.26 |    0.15 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 3           | 410.04 ns | 10.385 ns |  6.869 ns | 405.78 ns |  4.22 |    0.14 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 3           | 167.30 ns |  1.242 ns |  0.739 ns | 167.29 ns |  1.72 |    0.05 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 3           | 101.65 ns |  0.739 ns |  0.440 ns | 101.56 ns |  1.05 |    0.03 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 3           | 226.08 ns |  4.846 ns |  3.205 ns | 226.09 ns |  2.33 |    0.07 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 3           | 179.06 ns |  3.210 ns |  2.123 ns | 179.06 ns |  1.84 |    0.06 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     | 3           |  55.19 ns |  0.123 ns |  0.073 ns |  55.19 ns |  0.57 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     | 3           |  71.49 ns |  2.293 ns |  1.517 ns |  71.45 ns |  0.74 |    0.03 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 3           | 103.32 ns |  2.466 ns |  1.631 ns | 103.31 ns |  1.06 |    0.03 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 3           | 588.63 ns |  2.219 ns |  1.320 ns | 588.37 ns |  6.06 |    0.17 | 0.0248 |     416 B |        1.86 |
|                     |            |                |             |             |           |           |           |           |       |         |        |           |             |
| Email_Partial       | MediumRun  | 15             | 2           | 10          |  95.19 ns |  1.269 ns |  1.900 ns |  95.15 ns |  1.00 |    0.03 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | MediumRun  | 15             | 2           | 10          | 425.94 ns |  7.055 ns | 10.341 ns | 419.58 ns |  4.48 |    0.14 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | MediumRun  | 15             | 2           | 10          | 518.34 ns | 12.182 ns | 17.471 ns | 514.49 ns |  5.45 |    0.21 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | MediumRun  | 15             | 2           | 10          | 407.77 ns |  2.849 ns |  4.176 ns | 407.60 ns |  4.29 |    0.10 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | MediumRun  | 15             | 2           | 10          | 173.38 ns |  2.002 ns |  2.934 ns | 172.59 ns |  1.82 |    0.05 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | MediumRun  | 15             | 2           | 10          | 104.59 ns |  1.449 ns |  2.124 ns | 103.43 ns |  1.10 |    0.03 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | MediumRun  | 15             | 2           | 10          | 227.11 ns |  1.525 ns |  2.187 ns | 226.92 ns |  2.39 |    0.05 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | MediumRun  | 15             | 2           | 10          | 179.60 ns |  3.324 ns |  4.767 ns | 177.86 ns |  1.89 |    0.06 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | MediumRun  | 15             | 2           | 10          |  55.81 ns |  0.418 ns |  0.613 ns |  55.47 ns |  0.59 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | MediumRun  | 15             | 2           | 10          |  68.28 ns |  1.181 ns |  1.731 ns |  68.05 ns |  0.72 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Long          | MediumRun  | 15             | 2           | 10          | 104.72 ns |  0.872 ns |  1.251 ns | 104.86 ns |  1.10 |    0.03 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | MediumRun  | 15             | 2           | 10          | 576.37 ns | 15.192 ns | 20.795 ns | 559.80 ns |  6.06 |    0.25 | 0.0248 |     416 B |        1.86 |
