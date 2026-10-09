```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.46GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     |  54.53 ns |   1.274 ns |  0.843 ns |  1.00 |    0.02 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 229.40 ns |   7.461 ns |  4.935 ns |  4.21 |    0.11 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 271.55 ns |   5.163 ns |  3.415 ns |  4.98 |    0.09 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 229.04 ns |  13.920 ns |  9.207 ns |  4.20 |    0.17 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     |  98.98 ns |   3.116 ns |  2.061 ns |  1.82 |    0.04 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     |  59.60 ns |   1.458 ns |  0.867 ns |  1.09 |    0.02 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 127.82 ns |   3.594 ns |  2.377 ns |  2.34 |    0.05 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     |  96.53 ns |   1.790 ns |  0.936 ns |  1.77 |    0.03 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  38.46 ns |   0.777 ns |  0.514 ns |  0.71 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  39.49 ns |   1.163 ns |  0.769 ns |  0.72 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     |  56.49 ns |   1.089 ns |  0.648 ns |  1.04 |    0.02 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 362.15 ns |  22.602 ns | 14.950 ns |  6.64 |    0.28 | 0.0234 |     392 B |        1.75 |
|                     |            |                |             |           |            |           |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  55.22 ns |  13.338 ns |  0.731 ns |  1.00 |    0.02 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 233.45 ns |  85.200 ns |  4.670 ns |  4.23 |    0.09 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 277.82 ns | 196.025 ns | 10.745 ns |  5.03 |    0.18 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 225.35 ns | 195.132 ns | 10.696 ns |  4.08 |    0.17 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           |  97.90 ns |  17.198 ns |  0.943 ns |  1.77 |    0.03 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           |  60.74 ns |  68.658 ns |  3.763 ns |  1.10 |    0.06 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 129.94 ns |  19.402 ns |  1.063 ns |  2.35 |    0.03 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 100.68 ns |  60.765 ns |  3.331 ns |  1.82 |    0.06 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  38.25 ns |   6.864 ns |  0.376 ns |  0.69 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  39.78 ns |   5.218 ns |  0.286 ns |  0.72 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           |  56.91 ns |  26.966 ns |  1.478 ns |  1.03 |    0.03 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 344.79 ns | 154.243 ns |  8.455 ns |  6.24 |    0.15 | 0.0234 |     392 B |        1.75 |
