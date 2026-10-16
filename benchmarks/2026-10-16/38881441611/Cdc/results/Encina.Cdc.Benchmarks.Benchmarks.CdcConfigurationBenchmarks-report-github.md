```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error    | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |---------:|---------:|--------:|------:|--------:|-------:|----------:|------------:|
| AddEncinaCdc_Registration     | 361.0 ns | 26.41 ns | 1.45 ns |  3.54 |    0.04 | 0.0582 |     976 B |        2.60 |
| BuildConfigurationFluentChain | 101.9 ns | 21.76 ns | 1.19 ns |  1.00 |    0.01 | 0.0224 |     376 B |        1.00 |
