```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.16GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method               | Job        | IterationCount | LaunchCount | Mean     | Error   | StdDev  | Ratio | Allocated | Alloc Ratio |
|--------------------- |----------- |--------------- |------------ |---------:|--------:|--------:|------:|----------:|------------:|
| Generate_WithShardId | Job-YFEFPZ | 10             | Default     | 242.5 ns | 0.02 ns | 0.01 ns |  1.00 |         - |          NA |
| GenerateAndGetValue  | Job-YFEFPZ | 10             | Default     | 242.7 ns | 0.08 ns | 0.05 ns |  1.00 |         - |          NA |
| Generate             | Job-YFEFPZ | 10             | Default     | 242.7 ns | 0.31 ns | 0.18 ns |  1.00 |         - |          NA |
| ExtractShardId       | Job-YFEFPZ | 10             | Default     | 241.8 ns | 0.11 ns | 0.07 ns |  1.00 |         - |          NA |
|                      |            |                |             |          |         |         |       |           |             |
| Generate_WithShardId | ShortRun   | 3              | 1           | 242.6 ns | 1.69 ns | 0.09 ns |  1.00 |         - |          NA |
| GenerateAndGetValue  | ShortRun   | 3              | 1           | 242.7 ns | 1.20 ns | 0.07 ns |  1.00 |         - |          NA |
| Generate             | ShortRun   | 3              | 1           | 242.6 ns | 1.54 ns | 0.08 ns |  1.00 |         - |          NA |
| ExtractShardId       | ShortRun   | 3              | 1           | 241.7 ns | 0.37 ns | 0.02 ns |  1.00 |         - |          NA |
