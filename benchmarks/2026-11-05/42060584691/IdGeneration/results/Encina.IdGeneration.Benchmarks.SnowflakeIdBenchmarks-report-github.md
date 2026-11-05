```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method               | Job        | IterationCount | LaunchCount | Mean     | Error   | StdDev  | Ratio | Allocated | Alloc Ratio |
|--------------------- |----------- |--------------- |------------ |---------:|--------:|--------:|------:|----------:|------------:|
| Generate_WithShardId | Job-YFEFPZ | 10             | Default     | 242.3 ns | 0.05 ns | 0.03 ns |  1.00 |         - |          NA |
| GenerateAndGetValue  | Job-YFEFPZ | 10             | Default     | 242.3 ns | 0.03 ns | 0.02 ns |  1.00 |         - |          NA |
| Generate             | Job-YFEFPZ | 10             | Default     | 242.4 ns | 0.05 ns | 0.03 ns |  1.00 |         - |          NA |
| ExtractShardId       | Job-YFEFPZ | 10             | Default     | 241.2 ns | 0.03 ns | 0.02 ns |  1.00 |         - |          NA |
|                      |            |                |             |          |         |         |       |           |             |
| Generate_WithShardId | ShortRun   | 3              | 1           | 242.3 ns | 0.48 ns | 0.03 ns |  1.00 |         - |          NA |
| GenerateAndGetValue  | ShortRun   | 3              | 1           | 242.3 ns | 0.68 ns | 0.04 ns |  1.00 |         - |          NA |
| Generate             | ShortRun   | 3              | 1           | 242.3 ns | 0.49 ns | 0.03 ns |  1.00 |         - |          NA |
| ExtractShardId       | ShortRun   | 3              | 1           | 241.2 ns | 9.42 ns | 0.52 ns |  1.00 |         - |          NA |
