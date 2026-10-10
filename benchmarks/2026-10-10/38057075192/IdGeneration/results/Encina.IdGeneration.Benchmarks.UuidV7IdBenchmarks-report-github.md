```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,181.3 ns |   3.99 ns |  2.64 ns |  1.00 |    0.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   729.5 ns |  17.61 ns | 11.65 ns |  0.62 |    0.01 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,182.6 ns |   2.18 ns |  1.14 ns |  1.00 |    0.00 |         - |          NA |
|                    |            |                |             |            |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,247.8 ns |   8.11 ns |  0.44 ns |  0.98 |    0.02 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   721.6 ns |   4.35 ns |  0.24 ns |  0.57 |    0.01 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,271.7 ns | 656.78 ns | 36.00 ns |  1.00 |    0.03 |         - |          NA |
