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
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,143.3 ns |  6.16 ns | 3.67 ns |  1.01 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   721.1 ns |  1.61 ns | 0.96 ns |  0.64 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,129.1 ns |  1.38 ns | 0.72 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,119.4 ns |  4.95 ns | 0.27 ns |  0.93 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   721.1 ns | 11.07 ns | 0.61 ns |  0.60 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,208.8 ns | 25.94 ns | 1.42 ns |  1.00 |         - |          NA |
