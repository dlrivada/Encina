```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,125.0 ns |  4.40 ns | 2.62 ns |  0.99 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   718.0 ns |  3.04 ns | 1.81 ns |  0.63 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,136.3 ns |  2.78 ns | 1.45 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,114.1 ns | 19.59 ns | 1.07 ns |  0.93 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   713.6 ns | 10.54 ns | 0.58 ns |  0.60 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,198.7 ns | 44.08 ns | 2.42 ns |  1.00 |         - |          NA |
