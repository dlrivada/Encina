```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,147.8 ns |  7.19 ns | 4.28 ns |  0.94 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   613.1 ns |  2.13 ns | 1.41 ns |  0.50 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,226.8 ns |  4.37 ns | 2.28 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,185.1 ns | 88.49 ns | 4.85 ns |  1.02 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   610.0 ns |  7.12 ns | 0.39 ns |  0.52 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,167.6 ns | 58.34 ns | 3.20 ns |  1.00 |         - |          NA |
