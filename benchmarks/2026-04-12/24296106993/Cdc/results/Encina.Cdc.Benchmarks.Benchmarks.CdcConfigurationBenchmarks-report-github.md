```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                        | Mean     | Error   | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |---------:|--------:|---------:|------:|--------:|-------:|----------:|------------:|
| AddEncinaCdc_Registration     | 449.2 ns | 7.51 ns | 11.24 ns |  3.81 |    0.16 | 0.0582 |     976 B |        2.60 |
| BuildConfigurationFluentChain | 118.1 ns | 2.86 ns |  4.28 ns |  1.00 |    0.05 | 0.0224 |     376 B |        1.00 |
