```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  69.08 ns |  1.122 ns | 0.742 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 325.15 ns |  2.536 ns | 1.678 ns |  4.71 |    0.05 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 407.04 ns | 10.716 ns | 7.088 ns |  5.89 |    0.12 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 315.93 ns |  1.659 ns | 0.987 ns |  4.57 |    0.05 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 131.08 ns |  1.903 ns | 1.259 ns |  1.90 |    0.03 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     |  77.60 ns |  0.264 ns | 0.175 ns |  1.12 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 173.07 ns |  1.534 ns | 0.913 ns |  2.51 |    0.03 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 130.65 ns |  3.016 ns | 1.995 ns |  1.89 |    0.03 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  43.29 ns |  0.286 ns | 0.149 ns |  0.63 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  51.45 ns |  0.673 ns | 0.352 ns |  0.74 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     |  75.12 ns |  1.793 ns | 1.186 ns |  1.09 |    0.02 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 422.07 ns |  2.862 ns | 1.893 ns |  6.11 |    0.07 | 0.0248 |     416 B |        1.86 |
|                     |            |                |             |           |           |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  70.47 ns | 28.676 ns | 1.572 ns |  1.00 |    0.03 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 320.95 ns | 12.328 ns | 0.676 ns |  4.56 |    0.09 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 400.66 ns | 32.206 ns | 1.765 ns |  5.69 |    0.11 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 313.74 ns | 15.220 ns | 0.834 ns |  4.45 |    0.09 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 130.78 ns |  3.514 ns | 0.193 ns |  1.86 |    0.04 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           |  77.96 ns |  3.297 ns | 0.181 ns |  1.11 |    0.02 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 168.48 ns |  8.447 ns | 0.463 ns |  2.39 |    0.05 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 133.87 ns | 17.039 ns | 0.934 ns |  1.90 |    0.04 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  42.37 ns |  0.958 ns | 0.053 ns |  0.60 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  51.64 ns | 26.191 ns | 1.436 ns |  0.73 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           |  74.36 ns | 20.801 ns | 1.140 ns |  1.06 |    0.02 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 418.05 ns | 21.476 ns | 1.177 ns |  5.93 |    0.12 | 0.0248 |     416 B |        1.86 |
