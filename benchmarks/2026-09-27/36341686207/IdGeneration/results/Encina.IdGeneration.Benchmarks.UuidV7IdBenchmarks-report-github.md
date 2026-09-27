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
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,097.4 ns |  6.39 ns | 4.23 ns |  0.95 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   611.0 ns |  3.09 ns | 2.04 ns |  0.53 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,159.0 ns |  6.65 ns | 4.40 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,079.3 ns | 37.71 ns | 2.07 ns |  0.93 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   635.2 ns | 20.80 ns | 1.14 ns |  0.55 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,154.8 ns | 62.92 ns | 3.45 ns |  1.00 |         - |          NA |
