```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 827.2 ns |   3.87 ns | 2.56 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 389.2 ns |   4.45 ns | 2.65 ns |  0.47 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 830.0 ns |   6.89 ns | 4.10 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |           |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 828.3 ns | 133.32 ns | 7.31 ns |  1.01 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 384.6 ns |  11.01 ns | 0.60 ns |  0.47 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 821.4 ns |  73.98 ns | 4.05 ns |  1.00 |         - |          NA |
