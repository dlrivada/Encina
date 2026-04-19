```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]    : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                        | Mean     | Error   | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |---------:|--------:|--------:|------:|--------:|-------:|----------:|------------:|
| AddEncinaCdc_Registration     | 455.5 ns | 3.48 ns | 5.21 ns |  3.94 |    0.07 | 0.0386 |     976 B |        2.60 |
| BuildConfigurationFluentChain | 115.6 ns | 1.12 ns | 1.61 ns |  1.00 |    0.02 | 0.0148 |     376 B |        1.00 |
