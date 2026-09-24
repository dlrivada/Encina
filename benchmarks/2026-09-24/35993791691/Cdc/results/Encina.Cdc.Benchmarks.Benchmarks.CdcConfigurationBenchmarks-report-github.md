```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|--------:|------:|--------:|-------:|----------:|------------:|
| AddEncinaCdc_Registration     | 402.5 ns | 109.06 ns | 5.98 ns |  3.82 |    0.09 | 0.0114 |     976 B |        2.60 |
| BuildConfigurationFluentChain | 105.4 ns |  47.30 ns | 2.59 ns |  1.00 |    0.03 | 0.0044 |     376 B |        1.00 |
