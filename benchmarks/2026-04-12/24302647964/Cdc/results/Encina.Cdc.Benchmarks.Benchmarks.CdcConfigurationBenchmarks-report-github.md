```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                        | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| AddEncinaCdc_Registration     | 481.7 ns | 29.95 ns | 44.82 ns |  4.21 |    0.39 | 0.0582 |     976 B |        2.60 |
| BuildConfigurationFluentChain | 114.4 ns |  1.54 ns |  2.25 ns |  1.00 |    0.03 | 0.0224 |     376 B |        1.00 |
