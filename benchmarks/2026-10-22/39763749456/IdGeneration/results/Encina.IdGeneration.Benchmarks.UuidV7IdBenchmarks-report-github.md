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
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,116.4 ns |  3.58 ns | 2.13 ns |  0.98 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   613.3 ns |  2.07 ns | 1.23 ns |  0.54 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,136.7 ns |  1.10 ns | 0.65 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,215.5 ns | 41.10 ns | 2.25 ns |  1.06 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   631.2 ns |  7.52 ns | 0.41 ns |  0.55 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,147.8 ns | 25.43 ns | 1.39 ns |  1.00 |         - |          NA |
