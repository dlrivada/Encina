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
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,169.7 ns | 16.18 ns | 9.63 ns |  1.01 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   608.1 ns |  1.73 ns | 1.03 ns |  0.52 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,158.9 ns |  8.73 ns | 5.77 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,071.0 ns | 12.64 ns | 0.69 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   611.2 ns | 37.28 ns | 2.04 ns |  0.57 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,070.8 ns | 50.33 ns | 2.76 ns |  1.00 |         - |          NA |
