```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method               | Job        | IterationCount | LaunchCount | Mean     | Error   | StdDev  | Ratio | Allocated | Alloc Ratio |
|--------------------- |----------- |--------------- |------------ |---------:|--------:|--------:|------:|----------:|------------:|
| Generate_WithShardId | Job-YFEFPZ | 10             | Default     | 241.9 ns | 0.09 ns | 0.06 ns |  1.00 |         - |          NA |
| GenerateAndGetValue  | Job-YFEFPZ | 10             | Default     | 241.9 ns | 0.08 ns | 0.05 ns |  1.00 |         - |          NA |
| Generate             | Job-YFEFPZ | 10             | Default     | 241.9 ns | 0.09 ns | 0.06 ns |  1.00 |         - |          NA |
| ExtractShardId       | Job-YFEFPZ | 10             | Default     | 240.4 ns | 0.02 ns | 0.01 ns |  0.99 |         - |          NA |
|                      |            |                |             |          |         |         |       |           |             |
| Generate_WithShardId | ShortRun   | 3              | 1           | 241.8 ns | 2.35 ns | 0.13 ns |  1.00 |         - |          NA |
| GenerateAndGetValue  | ShortRun   | 3              | 1           | 241.8 ns | 2.06 ns | 0.11 ns |  1.00 |         - |          NA |
| Generate             | ShortRun   | 3              | 1           | 241.8 ns | 2.43 ns | 0.13 ns |  1.00 |         - |          NA |
| ExtractShardId       | ShortRun   | 3              | 1           | 240.1 ns | 0.94 ns | 0.05 ns |  0.99 |         - |          NA |
