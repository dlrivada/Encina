```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                        | Mean     | Error   | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |---------:|--------:|--------:|------:|--------:|-------:|----------:|------------:|
| AddEncinaCdc_Registration     | 460.7 ns | 2.55 ns | 3.81 ns |  3.63 |    0.05 | 0.0386 |     976 B |        2.60 |
| BuildConfigurationFluentChain | 126.8 ns | 0.90 ns | 1.33 ns |  1.00 |    0.01 | 0.0148 |     376 B |        1.00 |
