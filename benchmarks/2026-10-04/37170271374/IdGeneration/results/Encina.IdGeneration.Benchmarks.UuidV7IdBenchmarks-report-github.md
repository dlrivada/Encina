```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method             | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 3           | 753.7 ns | 19.09 ns | 12.62 ns |  0.99 |    0.02 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 3           | 374.3 ns |  4.62 ns |  3.05 ns |  0.49 |    0.01 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 3           | 758.4 ns | 14.25 ns |  8.48 ns |  1.00 |    0.02 |         - |          NA |
|                    |            |                |             |             |          |          |          |       |         |           |             |
| Generate_GetValue  | MediumRun  | 15             | 2           | 10          | 764.1 ns |  4.82 ns |  7.22 ns |  1.01 |    0.01 |         - |          NA |
| NewGuid_Comparison | MediumRun  | 15             | 2           | 10          | 369.9 ns |  2.11 ns |  3.16 ns |  0.49 |    0.01 |         - |          NA |
| Generate           | MediumRun  | 15             | 2           | 10          | 759.5 ns |  4.85 ns |  7.11 ns |  1.00 |    0.01 |         - |          NA |
