```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  6.661 ns | 0.9094 ns | 0.0498 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 37.389 ns | 2.0259 ns | 0.1110 ns |  5.61 |    0.04 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 30.850 ns | 2.3699 ns | 0.1299 ns |  4.63 |    0.03 | 0.0091 |     152 B |        6.33 |
