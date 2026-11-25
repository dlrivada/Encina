```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     26.96 ns |     0.936 ns |   0.051 ns |     1.22 |    0.04 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     22.05 ns |    14.553 ns |   0.798 ns |     1.00 |    0.04 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 53,683.38 ns | 5,797.355 ns | 317.773 ns | 2,437.04 |   75.90 | 0.3052 | 0.2441 |    5287 B |       47.21 |
| GenericTypeConstruction |     97.86 ns |     9.196 ns |   0.504 ns |     4.44 |    0.14 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     65.18 ns |     6.319 ns |   0.346 ns |     2.96 |    0.09 | 0.0105 |      - |     176 B |        1.57 |
