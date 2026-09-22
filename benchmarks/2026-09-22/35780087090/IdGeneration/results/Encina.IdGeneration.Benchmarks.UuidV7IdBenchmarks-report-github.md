```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,153.6 ns |  6.14 ns | 3.65 ns |  1.01 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   609.0 ns |  1.80 ns | 1.07 ns |  0.53 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,140.0 ns |  3.29 ns | 2.17 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,068.8 ns | 16.65 ns | 0.91 ns |  0.94 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   608.2 ns | 11.42 ns | 0.63 ns |  0.53 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,137.1 ns | 61.54 ns | 3.37 ns |  1.00 |         - |          NA |
