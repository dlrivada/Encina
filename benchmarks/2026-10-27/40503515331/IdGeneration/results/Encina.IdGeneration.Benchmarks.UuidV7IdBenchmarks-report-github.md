```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|---------:|---------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,129.7 ns | 16.95 ns | 11.21 ns |  0.99 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   612.3 ns |  1.44 ns |  0.85 ns |  0.54 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,136.7 ns |  6.21 ns |  3.69 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |          |          |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,204.9 ns | 34.64 ns |  1.90 ns |  1.05 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   611.0 ns |  7.86 ns |  0.43 ns |  0.53 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,143.6 ns | 61.28 ns |  3.36 ns |  1.00 |         - |          NA |
