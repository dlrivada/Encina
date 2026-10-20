```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| AddEncinaCdc_Registration     | 468.1 ns | 194.31 ns | 10.65 ns |  3.72 |    0.08 | 0.0582 |     976 B |        2.60 |
| BuildConfigurationFluentChain | 125.9 ns |  13.68 ns |  0.75 ns |  1.00 |    0.01 | 0.0224 |     376 B |        1.00 |
