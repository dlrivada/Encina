```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.83GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error    | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|---------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  8.679 ns | 1.597 ns | 0.0876 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 48.983 ns | 8.465 ns | 0.4640 ns |  5.64 |    0.07 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 39.981 ns | 1.344 ns | 0.0736 ns |  4.61 |    0.04 | 0.0091 |     152 B |        6.33 |
