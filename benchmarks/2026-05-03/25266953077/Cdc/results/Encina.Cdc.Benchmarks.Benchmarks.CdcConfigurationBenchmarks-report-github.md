```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]    : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                        | Mean     | Error   | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |---------:|--------:|--------:|------:|--------:|-------:|----------:|------------:|
| AddEncinaCdc_Registration     | 447.9 ns | 5.46 ns | 8.18 ns |  3.55 |    0.09 | 0.0582 |     976 B |        2.60 |
| BuildConfigurationFluentChain | 126.1 ns | 1.57 ns | 2.35 ns |  1.00 |    0.03 | 0.0224 |     376 B |        1.00 |
