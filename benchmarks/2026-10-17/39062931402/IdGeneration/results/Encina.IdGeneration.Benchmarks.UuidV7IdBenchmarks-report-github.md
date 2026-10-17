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
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,135.8 ns |  4.42 ns | 2.63 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   611.0 ns |  0.89 ns | 0.53 ns |  0.54 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,136.3 ns |  2.27 ns | 1.35 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,219.3 ns | 33.73 ns | 1.85 ns |  1.04 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   619.2 ns | 14.00 ns | 0.77 ns |  0.53 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,167.6 ns | 83.66 ns | 4.59 ns |  1.00 |         - |          NA |
