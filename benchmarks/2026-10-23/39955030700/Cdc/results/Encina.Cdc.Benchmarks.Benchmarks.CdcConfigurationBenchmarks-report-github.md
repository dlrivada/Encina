```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error    | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |---------:|---------:|--------:|------:|--------:|-------:|----------:|------------:|
| AddEncinaCdc_Registration     | 362.0 ns | 78.04 ns | 4.28 ns |  3.27 |    0.03 | 0.0582 |     976 B |        2.60 |
| BuildConfigurationFluentChain | 110.5 ns |  5.65 ns | 0.31 ns |  1.00 |    0.00 | 0.0224 |     376 B |        1.00 |
