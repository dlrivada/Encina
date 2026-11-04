```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.42GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  4.299 ns |  0.2721 ns | 0.0149 ns |  1.00 |    0.00 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 26.580 ns | 48.8733 ns | 2.6789 ns |  6.18 |    0.54 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 19.285 ns |  1.9848 ns | 0.1088 ns |  4.49 |    0.03 | 0.0091 |     152 B |        6.33 |
