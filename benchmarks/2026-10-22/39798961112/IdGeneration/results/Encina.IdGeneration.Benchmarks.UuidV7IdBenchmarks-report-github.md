```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method             | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 3           | 708.8 ns | 18.57 ns | 12.28 ns |  0.99 |    0.02 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 3           | 424.1 ns |  9.40 ns |  5.59 ns |  0.59 |    0.01 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 3           | 713.4 ns | 15.22 ns |  7.96 ns |  1.00 |    0.01 |         - |          NA |
|                    |            |                |             |             |          |          |          |       |         |           |             |
| Generate_GetValue  | MediumRun  | 15             | 2           | 10          | 677.6 ns |  7.85 ns | 11.76 ns |  0.97 |    0.03 |         - |          NA |
| NewGuid_Comparison | MediumRun  | 15             | 2           | 10          | 433.6 ns |  5.40 ns |  8.09 ns |  0.62 |    0.02 |         - |          NA |
| Generate           | MediumRun  | 15             | 2           | 10          | 697.1 ns | 12.39 ns | 18.16 ns |  1.00 |    0.04 |         - |          NA |
