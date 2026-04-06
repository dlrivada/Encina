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
| Generate           | Job-YFEFPZ | 10             | Default     | 1,145.1 ns |  3.31 ns | 2.19 ns |  1.00 |         - |          NA |
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,072.8 ns |  6.73 ns | 4.45 ns |  0.94 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   603.3 ns |  0.33 ns | 0.19 ns |  0.53 |         - |          NA |
|                    |            |                |             |            |          |         |       |           |             |
| Generate           | ShortRun   | 3              | 1           | 1,062.3 ns | 26.79 ns | 1.47 ns |  1.00 |         - |          NA |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,063.2 ns | 55.87 ns | 3.06 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   604.9 ns | 20.67 ns | 1.13 ns |  0.57 |         - |          NA |
