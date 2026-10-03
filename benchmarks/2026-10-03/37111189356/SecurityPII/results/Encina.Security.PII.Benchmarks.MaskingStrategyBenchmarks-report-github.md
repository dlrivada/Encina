```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     | 109.64 ns |  0.287 ns | 0.171 ns |  1.00 |    0.00 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 432.92 ns |  5.831 ns | 3.857 ns |  3.95 |    0.03 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 492.23 ns |  6.925 ns | 4.121 ns |  4.49 |    0.04 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 403.77 ns |  4.461 ns | 2.951 ns |  3.68 |    0.03 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 188.20 ns |  1.802 ns | 1.192 ns |  1.72 |    0.01 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 126.14 ns |  1.460 ns | 0.966 ns |  1.15 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 225.98 ns |  2.192 ns | 1.147 ns |  2.06 |    0.01 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 187.61 ns |  3.389 ns | 2.241 ns |  1.71 |    0.02 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  62.04 ns |  0.467 ns | 0.244 ns |  0.57 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  76.70 ns |  1.650 ns | 1.092 ns |  0.70 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 114.02 ns |  2.165 ns | 1.432 ns |  1.04 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 585.70 ns |  5.812 ns | 3.844 ns |  5.34 |    0.03 | 0.0248 |     416 B |        1.86 |
|                     |            |                |             |           |           |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           | 112.77 ns |  8.679 ns | 0.476 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 417.30 ns | 58.407 ns | 3.201 ns |  3.70 |    0.03 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 519.46 ns | 67.164 ns | 3.682 ns |  4.61 |    0.03 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 408.71 ns | 19.211 ns | 1.053 ns |  3.62 |    0.02 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 185.68 ns | 26.208 ns | 1.437 ns |  1.65 |    0.01 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           | 126.77 ns | 21.126 ns | 1.158 ns |  1.12 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 228.84 ns | 17.234 ns | 0.945 ns |  2.03 |    0.01 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 192.20 ns | 45.901 ns | 2.516 ns |  1.70 |    0.02 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  65.26 ns |  6.759 ns | 0.371 ns |  0.58 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  75.56 ns |  3.116 ns | 0.171 ns |  0.67 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           | 110.85 ns | 23.770 ns | 1.303 ns |  0.98 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 580.82 ns | 21.490 ns | 1.178 ns |  5.15 |    0.02 | 0.0248 |     416 B |        1.86 |
