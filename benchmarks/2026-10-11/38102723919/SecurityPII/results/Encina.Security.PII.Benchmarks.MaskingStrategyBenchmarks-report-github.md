```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method              | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error    | StdDev   | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |------------ |----------:|---------:|---------:|----------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     | 3           |  73.62 ns | 0.282 ns | 0.187 ns |  73.61 ns |  1.00 |    0.00 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 3           | 323.10 ns | 3.984 ns | 2.371 ns | 323.74 ns |  4.39 |    0.03 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 3           | 407.99 ns | 4.685 ns | 3.099 ns | 408.75 ns |  5.54 |    0.04 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 3           | 327.57 ns | 3.521 ns | 2.329 ns | 327.96 ns |  4.45 |    0.03 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 3           | 138.32 ns | 0.606 ns | 0.401 ns | 138.44 ns |  1.88 |    0.01 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 3           |  86.74 ns | 0.629 ns | 0.416 ns |  86.79 ns |  1.18 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 3           | 173.51 ns | 1.910 ns | 1.264 ns | 173.43 ns |  2.36 |    0.02 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 3           | 136.23 ns | 2.337 ns | 1.546 ns | 136.40 ns |  1.85 |    0.02 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     | 3           |  50.88 ns | 0.259 ns | 0.171 ns |  50.85 ns |  0.69 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     | 3           |  58.54 ns | 0.259 ns | 0.172 ns |  58.45 ns |  0.80 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 3           |  81.41 ns | 0.135 ns | 0.081 ns |  81.41 ns |  1.11 |    0.00 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 3           | 453.97 ns | 2.403 ns | 1.589 ns | 453.42 ns |  6.17 |    0.03 | 0.0234 |     392 B |        1.75 |
|                     |            |                |             |             |           |          |          |           |       |         |        |           |             |
| Email_Partial       | MediumRun  | 15             | 2           | 10          |  75.73 ns | 0.512 ns | 0.766 ns |  75.81 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | MediumRun  | 15             | 2           | 10          | 331.63 ns | 3.549 ns | 5.312 ns | 330.94 ns |  4.38 |    0.08 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | MediumRun  | 15             | 2           | 10          | 403.90 ns | 4.889 ns | 7.317 ns | 404.27 ns |  5.33 |    0.11 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | MediumRun  | 15             | 2           | 10          | 315.55 ns | 1.403 ns | 2.012 ns | 315.25 ns |  4.17 |    0.05 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | MediumRun  | 15             | 2           | 10          | 140.52 ns | 3.083 ns | 4.322 ns | 136.89 ns |  1.86 |    0.06 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | MediumRun  | 15             | 2           | 10          |  87.73 ns | 0.863 ns | 1.265 ns |  87.71 ns |  1.16 |    0.02 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | MediumRun  | 15             | 2           | 10          | 176.08 ns | 1.112 ns | 1.559 ns | 176.20 ns |  2.33 |    0.03 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | MediumRun  | 15             | 2           | 10          | 136.61 ns | 2.719 ns | 3.986 ns | 137.10 ns |  1.80 |    0.05 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | MediumRun  | 15             | 2           | 10          |  49.96 ns | 0.224 ns | 0.329 ns |  49.96 ns |  0.66 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | MediumRun  | 15             | 2           | 10          |  60.72 ns | 2.717 ns | 4.067 ns |  60.75 ns |  0.80 |    0.05 | 0.0076 |     128 B |        0.57 |
| Email_Long          | MediumRun  | 15             | 2           | 10          |  83.56 ns | 1.671 ns | 2.501 ns |  83.56 ns |  1.10 |    0.03 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | MediumRun  | 15             | 2           | 10          | 464.20 ns | 4.820 ns | 7.214 ns | 463.56 ns |  6.13 |    0.11 | 0.0234 |     392 B |        1.75 |
