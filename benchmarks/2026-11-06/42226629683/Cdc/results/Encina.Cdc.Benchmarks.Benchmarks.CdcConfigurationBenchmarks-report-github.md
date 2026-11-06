```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| AddEncinaCdc_Registration     | 433.2 ns | 341.34 ns | 18.71 ns |  3.81 |    0.14 | 0.0114 |     976 B |        2.60 |
| BuildConfigurationFluentChain | 113.7 ns |   6.33 ns |  0.35 ns |  1.00 |    0.00 | 0.0044 |     376 B |        1.00 |
