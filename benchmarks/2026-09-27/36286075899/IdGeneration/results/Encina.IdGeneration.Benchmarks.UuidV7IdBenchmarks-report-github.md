```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method             | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error    | StdDev   | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |------------ |-----------:|---------:|---------:|-----------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 3           | 1,098.5 ns | 12.22 ns |  8.08 ns | 1,099.6 ns |  1.01 |    0.01 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 3           |   610.6 ns |  1.52 ns |  0.90 ns |   610.4 ns |  0.56 |    0.00 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 3           | 1,088.4 ns |  4.88 ns |  3.23 ns | 1,087.5 ns |  1.00 |    0.00 |         - |          NA |
|                    |            |                |             |             |            |          |          |            |       |         |           |             |
| Generate_GetValue  | MediumRun  | 15             | 2           | 10          | 1,082.1 ns |  4.40 ns |  6.16 ns | 1,079.0 ns |  0.94 |    0.05 |         - |          NA |
| NewGuid_Comparison | MediumRun  | 15             | 2           | 10          |   610.3 ns |  0.85 ns |  1.17 ns |   610.9 ns |  0.53 |    0.03 |         - |          NA |
| Generate           | MediumRun  | 15             | 2           | 10          | 1,160.6 ns | 44.94 ns | 64.45 ns | 1,106.0 ns |  1.00 |    0.08 |         - |          NA |
