```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,167.3 ns |   4.65 ns |  2.43 ns |  1.08 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   612.1 ns |   1.81 ns |  1.20 ns |  0.57 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,078.0 ns |   3.89 ns |  2.57 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |           |          |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,064.0 ns |  24.04 ns |  1.32 ns |  0.98 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   610.7 ns |  23.90 ns |  1.31 ns |  0.57 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,080.4 ns | 191.96 ns | 10.52 ns |  1.00 |         - |          NA |
