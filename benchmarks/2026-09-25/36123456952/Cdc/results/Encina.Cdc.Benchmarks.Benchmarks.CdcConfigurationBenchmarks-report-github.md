```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|--------:|------:|--------:|-------:|----------:|------------:|
| AddEncinaCdc_Registration     | 445.5 ns | 114.19 ns | 6.26 ns |  3.65 |    0.13 | 0.0582 |     976 B |        2.60 |
| BuildConfigurationFluentChain | 122.3 ns |  82.18 ns | 4.50 ns |  1.00 |    0.05 | 0.0224 |     376 B |        1.00 |
