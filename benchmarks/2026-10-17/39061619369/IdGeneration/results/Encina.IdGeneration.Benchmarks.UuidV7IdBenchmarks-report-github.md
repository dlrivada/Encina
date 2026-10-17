```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,136.1 ns |  8.76 ns | 5.79 ns |  0.94 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   613.6 ns |  0.87 ns | 0.46 ns |  0.51 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,207.2 ns |  5.89 ns | 3.90 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,141.8 ns | 42.10 ns | 2.31 ns |  0.95 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   610.3 ns |  3.22 ns | 0.18 ns |  0.51 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,201.7 ns | 33.85 ns | 1.86 ns |  1.00 |         - |          NA |
