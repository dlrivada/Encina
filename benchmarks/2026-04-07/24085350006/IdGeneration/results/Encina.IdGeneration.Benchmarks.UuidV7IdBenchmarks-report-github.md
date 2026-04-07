```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,107.4 ns |  2.30 ns | 1.37 ns |  1.02 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   604.2 ns |  3.10 ns | 1.85 ns |  0.55 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,089.1 ns |  2.19 ns | 1.45 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,084.0 ns | 64.31 ns | 3.52 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   604.4 ns | 33.10 ns | 1.81 ns |  0.56 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,087.2 ns | 18.42 ns | 1.01 ns |  1.00 |         - |          NA |
