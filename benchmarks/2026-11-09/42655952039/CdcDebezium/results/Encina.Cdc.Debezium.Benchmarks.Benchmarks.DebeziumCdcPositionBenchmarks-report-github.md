```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  6.946 ns |  1.349 ns | 0.0739 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 38.428 ns | 29.073 ns | 1.5936 ns |  5.53 |    0.21 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 31.891 ns |  6.237 ns | 0.3418 ns |  4.59 |    0.06 | 0.0091 |     152 B |        6.33 |
