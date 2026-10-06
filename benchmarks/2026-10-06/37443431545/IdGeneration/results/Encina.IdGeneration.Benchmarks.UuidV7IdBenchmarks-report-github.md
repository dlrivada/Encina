```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|----------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,080.1 ns |   3.40 ns | 2.25 ns |  1.01 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   610.0 ns |   2.83 ns | 1.87 ns |  0.57 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,071.5 ns |   5.70 ns | 3.77 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |           |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,058.5 ns |  59.62 ns | 3.27 ns |  0.99 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   608.9 ns |  12.94 ns | 0.71 ns |  0.57 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,068.3 ns | 156.90 ns | 8.60 ns |  1.00 |         - |          NA |
