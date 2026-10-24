```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |---------:|---------:|--------:|------:|-------:|----------:|------------:|
| AddEncinaCdc_Registration     | 473.9 ns | 30.42 ns | 1.67 ns |  3.65 | 0.0114 |     976 B |        2.60 |
| BuildConfigurationFluentChain | 129.9 ns |  1.74 ns | 0.10 ns |  1.00 | 0.0043 |     376 B |        1.00 |
